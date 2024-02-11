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

public class RamsesBackingData : BackgroundService
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private readonly Channel<ProcessEntry> _bufferBlockChannel = Channel.CreateBounded<ProcessEntry>(1024);
	private readonly IServiceScopeFactory scopeFactory;
	private readonly IHttpClientFactory clientFactory;
	private readonly string RamsesVersion;
	private readonly string JbmVersion;

	public RamsesBackingData(IServiceScopeFactory scopeFactory, IHttpClientFactory clientFactory)
	{
		var verRam = typeof(RateMapSeveritySaber.Analyzer).Assembly.GetName().Version!;
		var verJbm = typeof(JBMConverter).Assembly.GetName().Version!;
		RamsesVersion = $"{verRam.Major}.{verRam.Minor}";
		JbmVersion = $"{verJbm.Major}.{verJbm.Minor}";
		this.scopeFactory = scopeFactory;
		this.clientFactory = clientFactory;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (var req in _bufferBlockChannel.Reader.ReadAllAsync(stoppingToken))
		{
			try
			{
				var res = await GetInternal(req);
				req.Task.SetResult(ToResult(res));
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
		var mapId = GetMapIdFromKey(key);
		if (mapId == null) return null;
		var req = new ProcessEntry(key, mapId.Value);
		if (!_bufferBlockChannel.Writer.TryWrite(req))
			return ToError("The request queue is full. Please wait a few minutes", HttpStatusCode.ServiceUnavailable);
		return await req.Task.Task;
	}

	public async IAsyncEnumerable<(long Id, BsMapProviderV2 Map)> GetMaps([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		using var scope = scopeFactory.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		await foreach (var entry in db.RamsesSongs
			.OrderByDescending(x => x.Id)
			.Select(x => new { x.Id, x.RawMap })
			.AsAsyncEnumerable()
			.WithCancellation(cancellationToken))
		{
			if (entry.RawMap is null)
			{
				continue;
			}

			var fileProvider = UnpackMap(entry.RawMap);
			yield return (entry.Id, fileProvider);
		}
	}

	private async Task<RamsesSong> GetInternal(ProcessEntry request)
	{
		using var scope = scopeFactory.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

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

		BSMapIO.FileProvider fileProvider;
		TimeSpan timeDownload = TimeSpan.Zero;
		TimeSpan timePackOrUnpack = TimeSpan.Zero;
		TimeSpan timeProcess = TimeSpan.Zero;

		if (entry is null || entry.RawMap is null)
		{
			var swDownload = Stopwatch.StartNew();
			using var client = clientFactory.CreateClient();
			using var response = await client.GetAsync($"https://beatsaver.com/api/download/key/{request.Key}");
			response.EnsureSuccessStatusCode();
			var data = await response.Content.ReadAsByteArrayAsync();
			timeDownload = swDownload.Elapsed;

			var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
			if (entry is null)
			{
				entry = new RamsesSongDto(request.MapId, RamsesVersion, JbmVersion);
				await db.RamsesSongs.AddAsync(entry);
			}
			var swPackOrUnpack = Stopwatch.StartNew();
			entry.JbmVersion = JbmVersion;
			entry.RawMap = PackMap(zip);
			timePackOrUnpack = swPackOrUnpack.Elapsed;
			fileProvider = BSMapIO.ZipProvider(zip);
		}
		else
		{
			var swPackOrUnpack = Stopwatch.StartNew();
			fileProvider = UnpackMap(entry.RawMap).AsBsMapProvider();
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

	private static long? GetMapIdFromKey(string key)
		=> long.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var mapId) ? mapId : null;

	class ProcessEntry(string key, long mapId)
	{
		public string Key { get; } = key;
		public long MapId { get; } = mapId;
		public TaskCompletionSource<IActionResult> Task { get; } = new();
	}

	public static byte[]? PackMap(ZipArchive sourceZip)
	{
		var sourceFiles = BSMapIO.ZipProvider(sourceZip);
		var jsonInfo = BSMapIO.ReadInfo(sourceFiles) ?? throw new Exception("No Info file found");

		var jbmOff = new JBMConverter(new JBMOptions() { UseDict = UseDict.Off, UseFloats = UseFloats.None });
		var jbm = new JBMConverter(new JBMOptions() { UseDict = UseDict.Simple, UseFloats = UseFloats.None });

		using var mem = new MemoryStream();
		using (var zip = new ZipArchive(mem, ZipArchiveMode.Create, true, Util.Utf8Encoding))
		{
			var mapDict = new Dictionary<string, (JsonElement Elem, JBMConverter Conv)>();

			void AddToCompressionDict(string file, JBMConverter conv, bool toDict)
			{
				using var fs = sourceFiles(file);
				if (fs is null) return;
				var json = JsonSerializer.Deserialize<JsonElement>(fs);
				mapDict.Add(file, (json, conv));
				if (toDict)
				{
					jbm.AddToDictionary(json);
				}
			}

			AddToCompressionDict("info.dat", jbmOff, false);
			AddToCompressionDict("info.json", jbmOff, false);

			foreach (var set in jsonInfo.DifficultyBeatmapSets)
				foreach (var maps in set.DifficultyBeatmaps)
					AddToCompressionDict(maps.BeatmapFilename, jbm, true);

			foreach (var (file, fileData) in mapDict)
			{
				var entry = zip.CreateEntry(file, CompressionLevel.NoCompression);
				using var writer = entry.Open();
				var data = fileData.Conv.EncodeEntity(fileData.Elem);
				writer.Write(data);
			}
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

	private static BsMapProviderV2 UnpackMap(byte[] data)
	{
		var output = new MemoryStream();
		using (var input = new MemoryStream(data))
		using (var decompressor = new BrotliStream(input, CompressionMode.Decompress))
		{
			decompressor.CopyTo(output);
		}
		output.Position = 0;
		var intermediateZip = new ZipArchive(output, ZipArchiveMode.Read);
		return new CompressedZipProvider(intermediateZip);
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
		return JBMConverter.EncodeObject(map, new JBMOptions() { UseDict = UseDict.Off, UseFloats = UseFloats.All, Compress = true });
	}

	public static RamsesMap UnpackScoreObject(byte[] data)
	{
		return JBMConverter.DecodeObject<RamsesMap>(data)!;
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
	[MapperIgnoreSource(nameof(RamsesSongDto.JbmVersion))]
	public static partial RamsesSong FromDto(this RamsesSongDto song);
	public static partial RamsesSong FromDto(this RamsesSongLightDto song);
	public static partial IQueryable<RamsesSongLightDto> MapToLight(this IQueryable<RamsesSongDto> song);
	[MapperIgnoreSource(nameof(RamsesSongDto.Id))]
	[MapperIgnoreSource(nameof(RamsesSongDto.RawMap))]
	private static partial RamsesSongLightDto MapToLight(this RamsesSongDto song);
	public static RamsesMap FromDto(this RamsesMapDto map) => RamsesBackingData.UnpackScoreObject(map.RatingDetail);
	private static partial List<RamsesMap> MapToList(List<RamsesMapDto> source);
}

public abstract class BsMapProviderV2
{
	public abstract IEnumerable<string> Files { get; }
	public abstract Stream? Get(string file);

	public BSMapIO.FileProvider AsBsMapProvider() => Get;
}

public class CompressedZipProvider(ZipArchive zip) : BsMapProviderV2
{
	public override IEnumerable<string> Files => zip.Entries.Select(e => e.FullName);
	public override Stream? Get(string file)
	{
		using var mem = new MemoryStream();
		using (var stream = zip.Entries.FirstOrDefault((ZipArchiveEntry e) => e.Name.Equals(file, StringComparison.OrdinalIgnoreCase))?.Open())
		{
			if (stream is null) return null;
			stream.CopyTo(mem);
		}
		return JBMConverter.DecodeToStream(mem.ToArray());
	}
}
