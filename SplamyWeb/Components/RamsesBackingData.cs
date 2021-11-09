using AutoMapper;
using JsonBinMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RateMapSeveritySaber;
using SplamyWeb.Db;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class RamsesBackingData
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private readonly Channel<ProcessEntry> _bufferBlockChannel = Channel.CreateBounded<ProcessEntry>(1024);
	private readonly IServiceScopeFactory scopeFactory;
	private readonly IHttpClientFactory clientFactory;
	private readonly IMapper mapper;
	private readonly string RamsesVersion;

	public RamsesBackingData(IServiceScopeFactory scopeFactory, IHttpClientFactory clientFactory, IMapper mapper)
	{
		var ver = typeof(Analyzer).Assembly.GetName().Version!;
		RamsesVersion = $"{ver.Major}.{ver.Minor}";
		this.scopeFactory = scopeFactory;
		this.clientFactory = clientFactory;
		this.mapper = mapper;
		_ = Process();
	}

	private async Task Process()
	{
		await foreach (var req in _bufferBlockChannel.Reader.ReadAllAsync())
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
			return ToError("The request queue is full. Please wait a few minutes");
		return await req.Task.Task;
	}

	private async Task<RamsesSong> GetInternal(ProcessEntry request)
	{
		using var scope = scopeFactory.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		var entry = await (from entries in db.RamsesSongs
						   where entries.Id == request.MapId
						   select entries)
					 .Include(e => e.Maps)
					 .SingleOrDefaultAsync();

		if (entry != null && entry.Version == RamsesVersion)
		{
			return mapper.Map<RamsesSong>(entry);
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
				entry = new RamsesSongDto(request.MapId, RamsesVersion);
				await db.RamsesSongs.AddAsync(entry);
			}
			var swPackOrUnpack = Stopwatch.StartNew();
			entry.RawMap = PackMap(zip);
			timePackOrUnpack = swPackOrUnpack.Elapsed;
			fileProvider = BSMapIO.ZipProvider(zip);
		}
		else
		{
			var swPackOrUnpack = Stopwatch.StartNew();
			fileProvider = UnpackMap(entry.RawMap);
			timePackOrUnpack = swPackOrUnpack.Elapsed;
		}

		entry.Version = RamsesVersion;
		if (entry.Maps.Count > 0)
		{
			entry.Maps.Clear();
			await db.SaveChangesAsync();
		}

		var swProcess = Stopwatch.StartNew();
		var maps = BSMapIO.Read(fileProvider);

		entry.Maps.AddRange(maps.Where(map => map.Characteristic == MapCharacteristic.Standard).Select(map =>
		{
			SongScore score;
			try
			{
				score = Analyzer.AnalyzeMap(map);
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Failed to analyze map '{0}'", request.Key);
				score = new SongScore(-1, -1, Array.Empty<AggregatedHit>());
			}

			var ramsesMap = ResultToJsonObject(score, map);
			var packedScore = PackScoreObject(ramsesMap);

			return new RamsesMapDto(
				map.Characteristic,
				(byte)map.DifficultyIndex,
				(byte)map.MapInfo.DifficultyRank,
				score.Average,
				packedScore);
		}));
		timeProcess = swProcess.Elapsed;

		Log.Info("RaMSeS Key:{0} Download:{1} (Un)Pack:{2} Process:{3} Cachesize:{4}", request.Key, timeDownload, timePackOrUnpack, timeProcess, entry.RawMap?.Length);

		await db.SaveChangesAsync();

		return mapper.Map<RamsesSong>(entry);
	}

	private static long? GetMapIdFromKey(string key)
		=> long.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var mapId) ? mapId : null;

	class ProcessEntry
	{
		public string Key { get; }
		public long MapId { get; }
		public TaskCompletionSource<IActionResult> Task { get; } = new();

		public ProcessEntry(string key, long mapId)
		{
			Key = key;
			MapId = mapId;
		}
	}

	private static byte[]? PackMap(ZipArchive sourceZip)
	{
		var sourceFiles = BSMapIO.ZipProvider(sourceZip);
		var jsonInfo = BSMapIO.ReadInfo(sourceFiles) ?? throw new Exception("No Info file found");

		var jbm = new JBMConverter(new() { UseFloats = UseFloats.None });

		using var mem = new MemoryStream();
		using (var zip = new ZipArchive(mem, ZipArchiveMode.Create, true, Util.Utf8Encoding))
		{
			var mapDict = new Dictionary<string, byte[]>();

			void AddToCompressionDict(string file)
			{
				using var mem = new MemoryStream();
				using var fs = sourceFiles(file);
				if (fs is null) return;
				fs.CopyTo(mem);
				var fileData = mem.ToArray();
				mapDict.Add(file, fileData);
				jbm.AddToDictionary(fileData);
			}

			AddToCompressionDict("info.dat");
			AddToCompressionDict("info.json");

			foreach (var set in jsonInfo.DifficultyBeatmapSets)
				foreach (var maps in set.DifficultyBeatmaps)
					AddToCompressionDict(maps.BeatmapFilename);

			foreach (var (file, fileData) in mapDict)
			{
				var entry = zip.CreateEntry(file, CompressionLevel.NoCompression);
				using var writer = entry.Open();
				writer.Write(jbm.EncodeEntity(fileData));
			}
		}
		mem.Position = 0;

		var output = new MemoryStream();
		using (var compressor = new BrotliStream(output, CompressionMode.Compress, true))
		{
			mem.CopyTo(compressor);
		}

		if (output.Length > 1_000_000)
		{
			Log.Warn("Compressed Map is >1MB (={0}B)", output.Length);
			return null;
		}

		return output.ToArray();
	}

	private static BSMapIO.FileProvider UnpackMap(byte[] data)
	{
		var output = new MemoryStream();
		using (var input = new MemoryStream(data))
		using (var decompressor = new BrotliStream(input, CompressionMode.Decompress))
		{
			decompressor.CopyTo(output);
		}
		output.Position = 0;
		var intermediateZip = new ZipArchive(output, ZipArchiveMode.Read);
		return (string file) =>
		{
			var mem = new MemoryStream();
			using (var stream = intermediateZip.Entries.FirstOrDefault((ZipArchiveEntry e) => e.Name.Equals(file, StringComparison.OrdinalIgnoreCase))?.Open())
			{
				if (stream is null) return null;
				stream.CopyTo(mem);
			}
			return JBMConverter.DecodeToStream(mem.ToArray());
		};
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
		return JBMConverter.EncodeObject(map, new JBMOptions() { UseDict = false, UseFloats = UseFloats.None, Compress = true });
	}

	public static RamsesMap UnpackScoreObject(byte[] data)
	{
		return JBMConverter.DecodeObject<RamsesMap>(data)!;
	}

	private static IActionResult ToResult(object content)
	{
		return new OkObjectResult(content);
	}

	private static IActionResult ToError(string error)
	{
		return new ObjectResult(new RamsesError(error))
		{
			StatusCode = (int)HttpStatusCode.BadRequest,
		};
	}
}

public class RamsesSong
{
	[JsonPropertyName("ramsesVersion")]
	public string Version { get; set; }
	[JsonPropertyName("maps")]
	public List<RamsesMap> Maps { get; set; }

	public RamsesSong(string version, List<RamsesMap> maps)
	{
		Version = version;
		Maps = maps;
	}
}

[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class RamsesMap
{
	[JsonPropertyName("difficulty")]
	public string Difficulty { get; set; }
	/// <summary>Internal mode name (Standard, 90°, 360°,...)</summary>
	[JsonPropertyName("characteristic")]
	public string Characteristic { get; set; }
	[JsonPropertyName("maxDifficulty")]
	public float MaxDifficulty { get; set; }
	[JsonPropertyName("avgDifficulty")]
	public float AvgDifficulty { get; set; }
	[JsonPropertyName("graph")]
	public float[] Graph { get; set; }

	public RamsesMap(string difficulty, string characteristic, float maxDifficulty, float avgDifficulty, float[] graph)
	{
		Difficulty = difficulty;
		Characteristic = characteristic;
		MaxDifficulty = maxDifficulty;
		AvgDifficulty = avgDifficulty;
		Graph = graph;
	}

	private string GetDebuggerDisplay() => $"{Characteristic}|{Difficulty}: Max:{MaxDifficulty} Avg:{AvgDifficulty}";
}

public class RamsesError
{
	[JsonPropertyName("error")]
	public string Error { get; set; }

	public RamsesError(string error)
	{
		Error = error;
	}
}

public class RamsesProfile : Profile
{
	public RamsesProfile()
	{
		CreateMap<RamsesSongDto, RamsesSong>(MemberList.Destination);
		CreateMap<RamsesMapDto, RamsesMap>(MemberList.None)
			.ConstructUsing(x => RamsesBackingData.UnpackScoreObject(x.RatingDetail))
			.ForAllMembers(opt => opt.Ignore());
	}
}
