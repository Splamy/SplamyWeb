using JsonBinMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RateMapSeveritySaber;
using Riok.Mapperly.Abstractions;
using SplamyWeb.Db;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public partial class RamsesBackingData : BackgroundService
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private readonly Channel<ProcessEntry> _bufferBlockChannel;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IHttpClientFactory _clientFactory;
	private readonly string RamsesVersion;
	private readonly string JbmVersion;

	private static readonly JBMOptions jbmMapOptions = new()
	{
		UseAos = true,
		UseJbm = false,
		Compress = false,
	};
	private static readonly JBMOptions jbmResultOptions = new()
	{
		UseAos = false,
		UseJbm = false,
		Compress = true,
	};

	public RamsesBackingData(IServiceScopeFactory scopeFactory, IHttpClientFactory clientFactory)
	{
		var verRam = typeof(RateMapSeveritySaber.Analyzer).Assembly.GetName().Version!;
		var verJbm = typeof(JBMConverter).Assembly.GetName().Version!;
		RamsesVersion = $"{verRam.Major}.{verRam.Minor}";
		JbmVersion = $"{verJbm.Major}.{verJbm.Minor}.a";
		_scopeFactory = scopeFactory;
		_clientFactory = clientFactory;

		_bufferBlockChannel = Channel.CreateBounded<ProcessEntry>(new BoundedChannelOptions(1024)
		{
			SingleReader = false,
			SingleWriter = false,
		});
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await MigrateDb(stoppingToken);

		await foreach (var req in _bufferBlockChannel.Reader.ReadAllAsync(stoppingToken))
		{
			try
			{
				var res = await GetInternal(req);
				req.Task.SetResult(ToResult(res));
			}
			catch (BeatsaverException ex)
			{
				var forwardError = new ContentResult() { StatusCode = (int)ex.Status, Content = ex.Message };
				req.Task.SetResult(forwardError);
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Failed to process song '{0}': {1}", req.MapId.ToString("X"), ex.Message);

				req.Task.SetResult(ToError(ex.Message));
			}
		}
	}

	public async Task<IActionResult?> Get(string key)
	{
		var mapId = MapKeyToId(key);
		if (mapId == null)
			return ToError("Invalid key", HttpStatusCode.BadRequest);
		var req = new ProcessEntry(key, mapId.Value);
		if (!_bufferBlockChannel.Writer.TryWrite(req))
			return ToError("The request queue is full. Please wait a few minutes", HttpStatusCode.ServiceUnavailable);
		return await req.Task.Task;
	}


	public async IAsyncEnumerable<(long Id, BsMapProvider Map)> GetMaps(
		ICollection<long> ids,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();
		await using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		await foreach (var entry in db.RamsesSongs
			.Where(x => x.RawMap != null)
			.Where(x => ids.Contains(x.Id))
			.Select(x => new { x.Id, x.RawMap })
			.AsAsyncEnumerable()
			.WithCancellation(cancellationToken))
		{
			var fileProvider = UnpackMap(entry.RawMap!);
			yield return (entry.Id, fileProvider);
		}
	}

	public async Task<string[]> FindMapsByQuery(
		Func<IQueryable<RamsesSongDto>, IQueryable<RamsesSongDto>> filter,
		CancellationToken cancellationToken = default)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();
		await using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		var query = db.RamsesSongs.AsQueryable();
		query = filter(query);

		var ids = await query
			.Where(x => x.RawMap != null)
			.Select(x => x.Id)
			.ToListAsync(cancellationToken);

		var keys = ids.Select(MapIdToKey).ToArray();

		return keys;
	}

	private async Task<RamsesSong> GetInternal(ProcessEntry request)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();
		await using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		var query = db.RamsesSongs
			.Where(entries => entries.Id == request.MapId)
			.Include(entries => entries.Maps);

		var entryLight = await query.MapToLight().FirstOrDefaultAsync();
		if (entryLight != null && entryLight.RamsesVersion == RamsesVersion)
		{
			return RamsesMapper.FromDto(entryLight);
		}

		RamsesSongDto? entry = null;
		if (entryLight != null)
		{
			entry = await query.FirstOrDefaultAsync();
		}

		BsMapProvider fileProvider;
		TimeSpan timeDownload = TimeSpan.Zero;
		TimeSpan timePackOrUnpack = TimeSpan.Zero;
		TimeSpan timeProcess = TimeSpan.Zero;

		if (entry is null)
		{
			var swDownload = Stopwatch.StartNew();
			using var client = _clientFactory.CreateClient();
			using var response = await client.GetAsync($"https://beatsaver.com/api/download/key/{request.Key}");
			if (!response.IsSuccessStatusCode)
			{
				var errorBody = await response.Content.ReadAsStringAsync();
				throw new BeatsaverException(errorBody, response.StatusCode);
			}
			var data = await response.Content.ReadAsByteArrayAsync();
			timeDownload = swDownload.Elapsed;

			var swPackOrUnpack = Stopwatch.StartNew();

			var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
			fileProvider = new PlainZipMapProvider(zip);

			using var infoFileFs = fileProvider.GetInfoFile() ?? throw new Exception("No Info file found");
			var info = JsonSerializer.Deserialize<JsonDocument>(infoFileFs, jbmMapOptions.JsonSerializerOptions)!;

			entry = new RamsesSongDto(
				request.MapId,
				RamsesVersion,
				JbmVersion,
				info,
				DateTimeOffset.UtcNow,
				PackMap(fileProvider));
			await db.RamsesSongs.AddAsync(entry);

			timePackOrUnpack = swPackOrUnpack.Elapsed;
		}
		else if (entry.RawMap is null)
		{
			throw new Exception("No map data found");
		}
		else
		{
			var swPackOrUnpack = Stopwatch.StartNew();
			fileProvider = UnpackMap(entry.RawMap);
			timePackOrUnpack = swPackOrUnpack.Elapsed;
		}

		entry.RamsesVersion = RamsesVersion;
		if (entry.Maps.Count > 0)
		{
			entry.Maps.Clear();
			await db.SaveChangesAsync();
		}

		var swProcess = Stopwatch.StartNew();
		var maps = BSMapIO.Read(fileProvider);

		entry.Maps = maps
			.Where(map => map.Characteristic == MapCharacteristic.Standard)
			.Select(map =>
			{
				SongScore score;
				try
				{
					score = RateMapSeveritySaber.Analyzer.AnalyzeMap(map);
				}
				catch (Exception ex)
				{
					Log.Warn(ex, "Failed to analyze map '{0}'", request.Key);
					score = new SongScore(-1, -1, []);
				}

				var ramsesMap = ResultToJsonObject(score, map);
				var packedScore = PackScoreObject(ramsesMap);

				return new RamsesMapDto(
					map.Characteristic,
					(byte)map.DifficultyIndex,
					(byte)map.MapInfo.DifficultyRank,
					score.Average,
					packedScore);
			})
			.ToList();
		timeProcess = swProcess.Elapsed;

		Log.Info("RaMSeS Key:{0} Download:{1} (Un)Pack:{2} Process:{3} Cachesize:{4}", request.Key, timeDownload, timePackOrUnpack, timeProcess, entry.RawMap?.Length);

		await db.SaveChangesAsync();

		return RamsesMapper.FromDto(entry);
	}

	public static long? MapKeyToId(string key)
		=> long.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var mapId) ? mapId : null;

	public static string MapIdToKey(long mapId)
		=> mapId.ToString("X", CultureInfo.InvariantCulture);

	class ProcessEntry(string key, long mapId)
	{
		public string Key { get; } = key;
		public long MapId { get; } = mapId;
		public TaskCompletionSource<IActionResult> Task { get; } = new();
	}

	public static byte[]? PackMap(BsMapProvider sourceFiles)
	{
		var jsonInfo = BSMapIO.ReadInfo(sourceFiles) ?? throw new Exception("No Info file found");

		using var mem = new MemoryStream();
		using (var zip = new ZipArchive(mem, ZipArchiveMode.Create, true, Util.Utf8Encoding))
		{
			void AddToCompressionDict(string file)
			{
				using var fs = sourceFiles.Get(file);
				if (fs is null) return;
				var entry = zip.CreateEntry(file, CompressionLevel.NoCompression);
				using var writer = entry.Open();
				var data = JBMConverter.Encode(JsonSerializer.Deserialize<JsonElement>(fs), jbmMapOptions);
				writer.Write(data);
			}

			AddToCompressionDict("info.dat");

			foreach (var set in jsonInfo.DifficultyBeatmapSets)
				foreach (var maps in set.DifficultyBeatmaps)
					AddToCompressionDict(maps.BeatmapFilename);
		}
		mem.Position = 0;

		using var output = new MemoryStream();
		using (var compressor = new BrotliStream(output, CompressionMode.Compress, true))
		{
			compressor.GetEncoder() = new BrotliEncoder(11, 24);
			mem.CopyTo(compressor);
		}

		if (output.Length > 1_000_000)
		{
			Log.Warn("Compressed Map is >1MB (={0}B)", output.Length);
			return null;
		}

		return output.ToArray();
	}

	private static BsMapProvider UnpackMap(byte[] data)
	{
		var output = new MemoryStream();
		using (var input = new MemoryStream(data))
		using (var decompressor = new BrotliStream(input, CompressionMode.Decompress))
		{
			decompressor.CopyTo(output);
		}
		output.Position = 0;
		var intermediateZip = new ZipArchive(output, ZipArchiveMode.Read);
		return new JbmZipProvider(intermediateZip, jbmMapOptions);
	}


	public static RamsesMap ResultToJsonObject(SongScore score, BSMap map)
	{
		return new RamsesMap(
			BSMapUtil.DifficultyNumberToName((byte)map.MapInfo.DifficultyRank),
			BSMapUtil.CharacteristicToName(map.Characteristic),
			score.Max,
			score.Average,
			score.Graph.Select(x => MathF.Round(x.TotalDifficulty(), 1)).ToArray()
		);
	}

	public static byte[] PackScoreObject(RamsesMap map)
	{
		return JBMConverter.EncodeObject(map, jbmResultOptions);
	}

	public static RamsesMap UnpackScoreObject(byte[] data)
	{
		return JBMConverter.DecodeObject<RamsesMap>(data, jbmResultOptions)!;
	}

	private static OkObjectResult ToResult(object content)
	{
		return new OkObjectResult(content);
	}

	private static ObjectResult ToError(string error, HttpStatusCode errorCode = HttpStatusCode.BadRequest)
	{
		return new ObjectResult(new RamsesError(error))
		{
			StatusCode = (int)errorCode,
		};
	}
}

public class RamsesSong(string ramsesVersion, List<RamsesMap> maps)
{
	[JsonPropertyName("ramsesVersion")]
	public string RamsesVersion { get; set; } = ramsesVersion;

	[JsonPropertyName("maps")]
	public List<RamsesMap> Maps { get; set; } = maps;
}

[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class RamsesMap(string difficulty, string characteristic, float maxDifficulty, float avgDifficulty, float[] graph)
{
	[JsonPropertyName("difficulty")]
	public string Difficulty { get; set; } = difficulty;
	/// <summary>Internal mode name (Standard, 90°, 360°,...)</summary>
	[JsonPropertyName("characteristic")]
	public string Characteristic { get; set; } = characteristic;
	[JsonPropertyName("maxDifficulty")]
	public float MaxDifficulty { get; set; } = maxDifficulty;
	[JsonPropertyName("avgDifficulty")]
	public float AvgDifficulty { get; set; } = avgDifficulty;
	[JsonPropertyName("graph")]
	public float[] Graph { get; set; } = graph;

	private string GetDebuggerDisplay() => $"{Characteristic}|{Difficulty}: Max:{MaxDifficulty} Avg:{AvgDifficulty}";
}

public class RamsesError(string error)
{
	[JsonPropertyName("error")]
	public string Error { get; set; } = error;
}

[Mapper]
public static partial class RamsesMapper
{
	[MapperIgnoreSource(nameof(RamsesSongDto.Id))]
	[MapperIgnoreSource(nameof(RamsesSongDto.RawMap))]
	[MapperIgnoreSource(nameof(RamsesSongDto.Info))]
	[MapperIgnoreSource(nameof(RamsesSongDto.JbmVersion))]
	public static partial RamsesSong FromDto(this RamsesSongDto song);
	[MapperIgnoreSource(nameof(RamsesSongDto.JbmVersion))]
	public static partial RamsesSong FromDto(this RamsesSongLightDto song);
	public static partial IQueryable<RamsesSongLightDto> MapToLight(this IQueryable<RamsesSongDto> song);
	[MapperIgnoreSource(nameof(RamsesSongDto.Id))]
	[MapperIgnoreSource(nameof(RamsesSongDto.RawMap))]
	[MapperIgnoreSource(nameof(RamsesSongDto.Info))]
	private static partial RamsesSongLightDto MapToLight(this RamsesSongDto song);
	public static RamsesMap FromDto(this RamsesMapDto map) => RamsesBackingData.UnpackScoreObject(map.RatingDetail);
	private static partial List<RamsesMap> MapToList(List<RamsesMapDto> source);
}

public class JbmZipProvider(ZipArchive zip, JBMOptions? options = null) : BsMapProvider
{
	public override IEnumerable<string> Files => zip.Entries.Select(e => NormalizeName(e.FullName));
	public override Stream? Get(string file)
	{
		using var mem = new MemoryStream();
		using (var stream = zip.Entries
			.FirstOrDefault((e) => MatchName(e.Name, file))?.Open())
		{
			if (stream is null) return null;
			stream.CopyTo(mem);
		}
		var arr = mem.GetBuffer().AsMemory(0, (int)mem.Length);
		return JBMConverter.DecodeToStream(arr, options);
	}
}

public class BeatsaverException(string message, HttpStatusCode status) : Exception(message)
{
	public HttpStatusCode Status { get; } = status;
}
