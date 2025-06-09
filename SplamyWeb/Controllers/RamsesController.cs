using Humanizer;
using Json.Logic;
using Json.More;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RateMapSeveritySaber;
using SplamyWeb.Components;
using SplamyWeb.Components.Ramses;
using SplamyWeb.Db;
using System.Formats.Tar;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RamsesController(RamsesBackingData ramses, SplamyContext db) : ControllerBase
{
	private const int CountLimit = 100;

	[HttpGet("m/{key}")]
	[HttpGet("map/{key}")]
	[Produces(MediaTypeNames.Application.Json)]
	public Task<IActionResult> GetMapRate(string key) => GetMapRateInternal(new AutoKey(key));

	[HttpGet("i/{key}/map")]
	[HttpGet("info/{key}/map")]
	[Produces(MediaTypeNames.Application.Json)]
	public async Task<IActionResult> GetMapMainInfo(string key)
	{
		if (new AutoKey(key).TryGetId() is not { } id)
			return BadRequest("Invalid key");

		var song = await db.RamsesSongs
			.Where(x => x.Id == id)
			.Select(x => x.Info)
			.FirstOrDefaultAsync();

		if (song is null)
			return NotFound();

		return Ok(song);
	}


	private async Task<IActionResult> GetMapRateInternal(AutoKey autoKey)
	{
		if (autoKey.TryGetKey() is not { } key)
			return BadRequest("Invalid key");

		var entry = await ramses.Get(key);
		if (entry is null)
			return NotFound();
		return entry;
	}

	[HttpPost("q/json")]
	[HttpPost("query/json")]
	//[Authorize(AuthenticationSchemes = AuthScheme)]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task<QueryResult> GetMatchJsonContainsZip(
		[FromBody] JsonElement json,
		CancellationToken cancellationToken
		)
	{
		return await FindMaps(r => r
			.Where(x => EF.Functions.JsonContains(x.Info, json)),
			cancellationToken);
	}

	[HttpPost("q/logic")]
	[HttpPost("query/logic")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task<QueryResult> GetMatchLogicZip(
		[FromBody] Rule jsonExpression,
		CancellationToken cancellationToken
	)
	{
		List<string> matches = [];

		await foreach (var song in db.RamsesSongs
			.Where(x => x.RawMap != null)
			.Select(x => new { x.Id, x.Info })
			.AsAsyncEnumerable()
			.WithCancellation(cancellationToken))
		{
			var result = jsonExpression.Apply(song.Info.RootElement.AsNode());
			if (result is JsonValue jsonValue && jsonValue.GetBool() == true)
			{
				matches.Add(RamsesBackingData.MapIdToKey(song.Id));
			}
		}

		return new QueryResult
		{
			Maps = matches,
		};
	}

	private async Task<QueryResult> FindMaps(
		Func<IQueryable<RamsesSong>, IQueryable<RamsesSong>> filter,
		CancellationToken cancellationToken)
	{
		var maps = await ramses.FindMapsByQuery(filter, cancellationToken);

		return new QueryResult
		{
			Maps = maps,
		};
	}

	[HttpPost("d")]
	[HttpPost("download")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	public Task DownloadMapsFromBody([FromForm] string keys, CancellationToken cancellationToken) => DownloadMaps(keys, cancellationToken);

	[HttpGet("d/{keys}")]
	[HttpGet("download/{keys}")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	public async Task DownloadMaps(
		string keys,
		CancellationToken cancellationToken)
	{
		Response.Headers.ContentDisposition = "attachment; filename=maps.tar.gz";

		var stream = Response.Body;

		await using var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
		await using var tar = new TarWriter(gzip, leaveOpen: true);

		var ids = keys
			.Split(',')
			.Select(x => new AutoKey(x).TryGetId())
			.Where(x => x is not null)
			.Select(x => x!.Value)
			.ToArray();

		await foreach (var (id, provider) in ramses.GetMaps(ids, cancellationToken))
		{
			var mapIdStr = RamsesBackingData.MapIdToKey(id);

			foreach (var file in provider.Files)
			{
				await using var fileStream = provider.Get(file);

				if (fileStream is null)
					continue;

				var entry = new V7TarEntry(TarEntryType.V7RegularFile, mapIdStr + "/" + file)
				{
					Mode = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead | UnixFileMode.UserWrite | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite,
					DataStream = fileStream,
				};

				await tar.WriteEntryAsync(entry, cancellationToken);
			}
		}
	}

	[HttpGet("system")]
	public async Task<RamsesDbMetadata> GetDbMetadata(CancellationToken cancellationToken)
	{
		var indexedSongs = await db.RamsesSongs.CountAsync(cancellationToken);
		var indexedDifficulties = await db.RamsesMaps.CountAsync(cancellationToken);
		var totalSize = await db.RamsesSongs.Where(x => x.RawMap != null).SumAsync(x => x.RawMap!.Length, cancellationToken);

		return new RamsesDbMetadata
		{
			IndexedSongs = indexedSongs,
			IndexedDifficulties = indexedDifficulties,
			TotalSize = totalSize,
		};
	}

	[HttpGet("system/display")]
	public async Task<object> GetDbMetadataDisplay(CancellationToken cancellationToken)
	{
		var meta = await GetDbMetadata(cancellationToken);

		return new
		{
			IndexedSongs = TabController.FormatMetric((uint)meta.IndexedSongs),
			IndexedDifficulties = TabController.FormatMetric((uint)meta.IndexedDifficulties),
			TotalSize = meta.TotalSize.Bytes().Humanize("#.#", CultureInfo.InvariantCulture),
		};
	}

	[HttpGet("pattern/parse")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	public string GetPatternSentence([FromBody] string[] matrix, CancellationToken cancellationToken)
	{
		var frames = matrix.Select(RamsesNoteFrame.ParseRadableFrame).ToArray();
		var searchString = string.Join(" ", frames.Select(f => f.ToWord()));
		return searchString;
	}

	[HttpPost("q/pattern")]
	[HttpPost("query/pattern")]
	//[Authorize(AuthenticationSchemes = AuthScheme)]
	public async Task<ActionResult<QueryResult>> PatternSearch([FromBody] string[] matrix, CancellationToken cancellationToken)
	{
		string searchString;
		try
		{
			var frames = matrix.Select(RamsesNoteFrame.ParseRadableFrame);
			searchString = string.Join("<->", frames.Select(f => f.ToWord()));
		}
		catch (Exception ex)
		{
			return BadRequest("Invalid pattern format: " + ex.Message);
		}

		var maps = await db.RamsesMaps
			.Where(x => x.SearchVector != null && x.SearchVector.Matches(EF.Functions.ToTsQuery("simple", searchString)))
			.Select(x => x.RamsesId)
			.Distinct()
			.ToListAsync(cancellationToken);

		return new QueryResult() { Maps = [.. maps.Select(RamsesBackingData.MapIdToKey)] };
	}

	[HttpPost("updateindex")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	public async Task<IActionResult> UpdateIndex([FromServices] ILogger<RamsesController> logger)
	{
		Dictionary<long, List<int>> invertedLookup = [];

		int cnt = 0;
		bool hasMore = false;

		while (true)
		{
			try
			{
				hasMore = false;
				var batch = await db.RamsesSongs
					.Where(x => x.RawMap != null)
					.Where(x => x.Maps.Any(m => m.SearchIndexVersion != RamsesBackingData.RamsesQueryVersion))
					.Include(x => x.Maps)
					.Take(10)
					.ToListAsync();
				hasMore = batch.Count > 0;

				foreach (var entry in batch)
				{
					var provider = RamsesBackingData.UnpackMap(entry.RawMap!);
					var mapData = BSMapIO.Read(provider);

					foreach (var diff in entry.Maps)
					{
						var dataDiff = mapData.FirstOrDefault(x => x.Characteristic == diff.Characteristic && x.DifficultyIndex == diff.IndexDifficulty);
						if (dataDiff is null)
							continue;


						var vector = await RamsesBackingData.ToRamsesVector(db, dataDiff);
						diff.SearchVector = vector;
						diff.SearchIndexVersion = RamsesBackingData.RamsesQueryVersion;
					}

					cnt++;
					logger.LogInformation("Processed {Count} maps", cnt);
				}
			}
			catch (Exception ex)
			{
				logger.LogInformation(ex, "fail");
				return Problem();
			}
			finally
			{
				logger.LogInformation("Saving");
				await db.SaveChangesAsync();
				db.ChangeTracker.Clear();
				logger.LogInformation("Done");
			}

			if (!hasMore)
				break;
		}

		return Ok();
	}

}

public class RamsesDbMetadata
{
	public int IndexedSongs { get; set; }
	public int IndexedDifficulties { get; set; }
	public long TotalSize { get; set; }
}

public class QueryResult
{
	public IReadOnlyList<string> Maps { get; set; } = [];
}

public readonly record struct AutoKey
{
	private readonly string? _keyOrId;
	private readonly long? _id;

	public AutoKey(string key)
	{
		_keyOrId = key;
		_id = null;
	}

	public AutoKey(long id)
	{
		_keyOrId = null;
		_id = id;
	}

	public readonly string? TryGetKey()
	{
		if (_keyOrId is not null)
		{
			if (RamsesBackingData.MapKeyToId(_keyOrId[1..]) is { } id)
				return RamsesBackingData.MapIdToKey(id);
			else
				return null;
		}
		if (_id is not null)
		{
			return RamsesBackingData.MapIdToKey(_id.Value);
		}
		return null;
	}

	public readonly long? TryGetId()
	{
		if (_id is not null)
			return _id;
		if (_keyOrId is not null)
		{
			if (RamsesBackingData.MapKeyToId(_keyOrId[1..]) is { } id)
				return id;
			else
				return RamsesBackingData.MapKeyToId(_keyOrId);
		}
		return null;
	}
}
