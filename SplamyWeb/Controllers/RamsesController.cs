using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.IO.Compression;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Formats.Tar;
using Microsoft.AspNetCore.Authorization;
using static SplamyWeb.Util;
using System.Threading;
using SplamyWeb.Db;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Json.Logic;
using Json.More;
using System.Text.Json.Nodes;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RamsesController(RamsesBackingData ramses, SplamyContext db) : ControllerBase
{
	private const int CountLimit = 100;

	[HttpGet("m/{key}")]
	[Produces(MediaTypeNames.Application.Json)]
	public Task<IActionResult> GetMapRate(string key) => GetMapRateInternal(key);

	[HttpGet("mi/{id}")]
	[Produces(MediaTypeNames.Application.Json)]
	public Task<IActionResult> GetMapRate(long id) => GetMapRateInternal(RamsesBackingData.MapIdToKey(id));

	private async Task<IActionResult> GetMapRateInternal(string key)
	{
		var entry = await ramses.Get(key);
		if (entry is null)
			return NotFound();
		return entry;
	}

	[HttpGet("q/take/{count}")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	public async Task GetMapsZip(int count, CancellationToken cancellationToken)
	{
		count = Math.Clamp(count, 0, CountLimit);
		await QueryMaps(r => r.Take(count), cancellationToken);
	}

	[HttpGet("q/json")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task GetMatchJsonContainsZip(
		CancellationToken cancellationToken,
		[FromBody] JsonElement json,
		[FromQuery] int? skip,
		[FromQuery] int? take = null
		)
	{
		var skipv = Math.Clamp(skip ?? 0, 0, int.MaxValue);
		var takev = Math.Clamp(take ?? CountLimit, 0, CountLimit);

		await QueryMaps(r => r
			.Where(x => EF.Functions.JsonContains(x.Info, json))
			.Skip(skipv)
			.Take(takev),
			cancellationToken);
	}

	[HttpGet("q/logic")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task GetMatchLogicZip(
		CancellationToken cancellationToken,
		[FromBody] Rule jsonExpression,
		[FromQuery] int? skip,
		[FromQuery] int? take = null
	)
	{
		var skipv = Math.Clamp(skip ?? 0, 0, int.MaxValue);
		var takev = Math.Clamp(take ?? CountLimit, 0, CountLimit);

		List<long> matches = [];

		await foreach (var song in db.RamsesSongs
			.Select(x => new { x.Id, x.Info })
			.AsAsyncEnumerable()
			.WithCancellation(cancellationToken))
		{
			var result = jsonExpression.Apply(song.Info.RootElement.AsNode());
			if (result is JsonValue jsonValue && jsonValue.GetBool() == true)
			{
				matches.Add(song.Id);
			}
		}

		await QueryMaps(r => r
			.Where(x => matches.Contains(x.Id))
			.Skip(skipv)
			.Take(takev),
			cancellationToken);
	}

	public async Task QueryMaps(Func<IQueryable<RamsesSongDto>, IQueryable<RamsesSongDto>> filter, CancellationToken cancellationToken)
	{
		Response.Headers.ContentDisposition = "attachment; filename=maps.tar.gz";

		var stream = Response.Body;

		await using var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
		await using var tar = new TarWriter(gzip, leaveOpen: true);

		await foreach (var (id, provider) in ramses.GetMapsByQuery(filter, cancellationToken))
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

	[HttpGet("info")]
	public async Task<RamsesDbMetadata> GetDbMetadata(CancellationToken cancellationToken)
	{
		var indexedSongs = await db.RamsesSongs.CountAsync(cancellationToken);
		var indexedMaps = await db.RamsesMaps.CountAsync(cancellationToken);

		return new RamsesDbMetadata
		{
			IndexedSongs = indexedSongs,
			IndexedMaps = indexedMaps,
		};
	}

	public class RamsesDbMetadata
	{
		public int IndexedSongs { get; set; }
		public int IndexedMaps { get; set; }
	}
}
