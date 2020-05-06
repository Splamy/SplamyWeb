using LiteDB;
using RateMapSeveritySaber;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SplamyWeb.Components
{
	public class RamsesBackingData
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly BufferBlock<(string, TaskCompletionSource<RamsesEntry?>)> _bufferBlock = new BufferBlock<(string, TaskCompletionSource<RamsesEntry?>)>();
		private readonly ILiteCollection<RamsesEntry> ramsesTable;
		private readonly Task processTask;

		private readonly string RamsesVersion;

		public RamsesBackingData(LocalDb db)
		{
			ramsesTable = db.RamsesTable;
			var ver = typeof(Analyzer).Assembly.GetName().Version!;
			RamsesVersion = $"{ver.Major}.{ver.Minor}";
			processTask = Process();
		}

		private async Task Process()
		{
			while (true)
			{
				var (key, response) = await _bufferBlock.ReceiveAsync();
				RamsesEntry? res;
				try
				{
					res = await GetInternal(key);
				}
				catch (Exception ex)
				{
					Log.Warn(ex, "Failed to process song: {0}", ex.Message);

					res = new RamsesEntry
					{
						Id = key,
						Version = RamsesVersion,
						Maps = Array.Empty<RamsesMap>(),
					};
				}
				response.SetResult(res);
			}
		}

		public async Task<RamsesEntry?> Get(string key)
		{
			var tcs = new TaskCompletionSource<RamsesEntry?>();
			await _bufferBlock.SendAsync((key, tcs));
			return await tcs.Task;
		}

		private async Task<RamsesEntry?> GetInternal(string key)
		{
			var entry = ramsesTable.FindById(key);
			if (entry != null && entry.Version == RamsesVersion)
				return entry;

			var sw = Stopwatch.StartNew();
			using var data = await Util.httpClient.GetAsync($"https://beatsaver.com/api/download/key/{key}");
			data.EnsureSuccessStatusCode();
			using var stream = await data.Content.ReadAsStreamAsync();
			using var zip = new ZipArchive(stream);
			var maps = BSMapIO.Read(file =>
			{
				var infoE = zip.GetEntry(file);
				return infoE.Open();
			});
			var timeToDownload = sw.Elapsed;

			sw.Restart();
			var parsedMaps = maps.Select(map =>
			{
				Score score;
				try
				{
					score = Analyzer.AnalyzeMap(map);
				}
				catch (Exception ex)
				{
					Log.Warn(ex, "Failed to analyze map '{0}'", key);
					score = new Score
					{
						Avg = -1,
						Max = -1,
						Graph = Array.Empty<float>(),
					};
				}

				return new RamsesMap
				{
					AvgDifficulty = score.Avg,
					MaxDifficulty = score.Max,
					Graph = score.Graph,
					Difficulty = map.MapInfo._difficulty,
				};
			}).ToArray();
			var timeToProcess = sw.Elapsed;

			Log.Info("RaMSeS Key:{0} Download:{1} Process{2}", key, timeToDownload, timeToProcess);

			entry = new RamsesEntry
			{
				Id = key,
				Maps = parsedMaps,
				Version = RamsesVersion,
			};

			ramsesTable.Upsert(entry);

			return entry;
		}
	}

#pragma warning disable CS8618
	public class RamsesEntry
	{
		[JsonIgnore]
		public string Id { get; set; }
		[JsonPropertyName("ramsesVersion")]
		public string Version { get; set; }
		[JsonPropertyName("maps")]
		public RamsesMap[] Maps { get; set; }
	}

	public class RamsesMap
	{
		[JsonPropertyName("difficulty")]
		public string Difficulty { get; set; }
		[JsonPropertyName("maxDifficulty")]
		public float MaxDifficulty { get; set; }
		[JsonPropertyName("avgDifficulty")]
		public float AvgDifficulty { get; set; }
		[JsonPropertyName("graph")]
		public float[] Graph { get; set; }
	}
#pragma warning restore CS8618
}
