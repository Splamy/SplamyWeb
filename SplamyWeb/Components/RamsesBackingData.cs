using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RateMapSeveritySaber;
using SplamyWeb.Db;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class RamsesBackingData
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly Channel<ProcessEntry> _bufferBlockChannel = Channel.CreateBounded<ProcessEntry>(1024);
		private readonly IServiceScopeFactory scopeFactory;

		private readonly string RamsesVersion;

		public RamsesBackingData(IServiceScopeFactory scopeFactory)
		{
			var ver = typeof(Analyzer).Assembly.GetName().Version!;
			RamsesVersion = $"{ver.Major}.{ver.Minor}";
			this.scopeFactory = scopeFactory;
			_ = Process();
		}

		private async Task Process()
		{
			await foreach (var req in _bufferBlockChannel.Reader.ReadAllAsync())
			{
				RamsesSong? res;
				try
				{
					res = await GetInternal(req);
				}
				catch (Exception ex)
				{
					Log.Warn(ex, "Failed to process song: {0}", ex.Message);

					res = new RamsesSong(req.MapId, RamsesVersion) { Error = ex.Message };
				}
				req.Task.SetResult(res);
			}
		}

		public async Task<RamsesSong?> Get(string key)
		{
			var mapId = GetMapIdFromKey(key);
			if (mapId == null) return null;
			var req = new ProcessEntry(key, mapId.Value);
			if (!_bufferBlockChannel.Writer.TryWrite(req))
				return null; // Queue is full
			return await req.Task.Task;
		}

		private async Task<RamsesSong?> GetInternal(ProcessEntry request)
		{
			using var scope = scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

			var entry = await (from entries in db.RamsesSongs
							   where entries.Id == request.MapId
							   select entries)
						 .Include(e => e.Maps)
						 .SingleOrDefaultAsync();

			if (entry != null && entry.Version == RamsesVersion)
				return entry;

			ZipArchive zip;
			TimeSpan timeDownload = TimeSpan.Zero;
			TimeSpan timePackOrUnpack = TimeSpan.Zero;
			TimeSpan timeProcess = TimeSpan.Zero;

			if (entry is null || entry.RawMap is null)
			{
				var swDownload = Stopwatch.StartNew();
				using var response = await Util.httpClient.GetAsync($"https://beatsaver.com/api/download/key/{request.Key}");
				response.EnsureSuccessStatusCode();
				var data = await response.Content.ReadAsByteArrayAsync();
				timeDownload = swDownload.Elapsed;

				zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
				if (entry is null)
				{
					entry = new RamsesSong(request.MapId, RamsesVersion);
					await db.RamsesSongs.AddAsync(entry);
				}
				var swPackOrUnpack = Stopwatch.StartNew();
				entry.RawMap = PackMap(zip);
				timePackOrUnpack = swPackOrUnpack.Elapsed;
			}
			else
			{
				var swPackOrUnpack = Stopwatch.StartNew();
				zip = UnpackMap(entry.RawMap);
				timePackOrUnpack = swPackOrUnpack.Elapsed;
			}

			var swProcess = Stopwatch.StartNew();
			var maps = BSMapIO.ReadZip(zip);
			entry.Maps.Clear();
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

				return new RamsesMap(
					map.Characteristic,
					(byte)map.DifficultyIndex,
					(byte)map.MapInfo.DifficultyRank,
					score.Max,
					score.Average,
					score.Graph.Select(x => x.TotalDifficulty()).ToArray());
			}));
			timeProcess = swProcess.Elapsed;

			Log.Info("RaMSeS Key:{0} Download:{1} (Un)Pack:{2} Process:{3} Cachesize:{4}", request.Key, timeDownload, timePackOrUnpack, timeProcess, entry.RawMap?.Length);

			await db.SaveChangesAsync();

			return entry;
		}

		private static long? GetMapIdFromKey(string key)
			=> long.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var mapId) ? mapId : null;

		class ProcessEntry
		{
			public string Key { get; }
			public long MapId { get; }
			public TaskCompletionSource<RamsesSong?> Task { get; }

			public ProcessEntry(string key, long mapId)
			{
				Key = key;
				MapId = mapId;
				Task = new TaskCompletionSource<RamsesSong?>();
			}
		}

		private static byte[]? PackMap(ZipArchive sourceZip)
		{
			var sourceFiles = BSMapIO.ZipProvider(sourceZip);
			var jsonInfo = BSMapIO.ReadInfo(sourceFiles) ?? throw new Exception("No Info file found");

			using var mem = new MemoryStream();
			using (var zip = new ZipArchive(mem, ZipArchiveMode.Create, true, Util.Utf8Encoding))
			{
				void TryCopyFile(string file)
				{
					var entry = zip.CreateEntry(file, CompressionLevel.NoCompression);
					using var writer = entry.Open();
					using var fs = sourceFiles(file);
					if (fs is null) return;
					fs.CopyTo(writer);
				}

				TryCopyFile("info.dat");
				TryCopyFile("info.json");

				foreach (var set in jsonInfo.DifficultyBeatmapSets)
					foreach (var maps in set.DifficultyBeatmaps)
						TryCopyFile(maps.BeatmapFilename);
			}
			mem.Seek(0, SeekOrigin.Begin);

			var output = new MemoryStream();
			using (var compressor = new BrotliStream(output, CompressionMode.Compress, true))
			{
				mem.CopyTo(compressor);
			}
			output.Seek(0, SeekOrigin.Begin);

			if (output.Length > 1_000_000)
			{
				Log.Warn("Compressed Map is >1MB (={0}B)", output.Length);
				return null;
			}

			return output.ToArray();
		}

		private static ZipArchive UnpackMap(byte[] data)
		{
			var output = new MemoryStream();
			using (var input = new MemoryStream(data))
			using (var decompressor = new BrotliStream(input, CompressionMode.Decompress))
			{
				decompressor.CopyTo(output);
			}
			output.Seek(0, SeekOrigin.Begin);
			return new ZipArchive(output, ZipArchiveMode.Read);
		}
	}
}
