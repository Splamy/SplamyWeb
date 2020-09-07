using LiteDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RateMapSeveritySaber;
using SplamyWeb.Db;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SplamyWeb.Components
{
	public class RamsesBackingData
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly BufferBlock<ProcessEntry> _bufferBlock = new BufferBlock<ProcessEntry>();
		private readonly IServiceScopeFactory scopeFactory;
		private readonly Task processTask;

		private readonly string RamsesVersion;

		public RamsesBackingData(IServiceScopeFactory scopeFactory)
		{
			var ver = typeof(Analyzer).Assembly.GetName().Version!;
			RamsesVersion = $"{ver.Major}.{ver.Minor}";
			processTask = Process();
			this.scopeFactory = scopeFactory;
		}

		private async Task Process()
		{
			while (true)
			{
				var req = await _bufferBlock.ReceiveAsync();
				RamsesSong? res;
				try
				{
					res = await GetInternal(req);
				}
				catch (Exception ex)
				{
					Log.Warn(ex, "Failed to process song: {0}", ex.Message);

					res = new RamsesSong(req.MapId, RamsesVersion);
				}
				req.Task.SetResult(res);
			}
		}

		public async Task<RamsesSong?> Get(string key)
		{
			var mapId = GetMapIdFromKey(key);
			if (mapId == null) return null;
			var req = new ProcessEntry(key, mapId.Value);
			await _bufferBlock.SendAsync(req);
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

			var sw = Stopwatch.StartNew();
			using var data = await Util.httpClient.GetAsync($"https://beatsaver.com/api/download/key/{request.Key}");
			data.EnsureSuccessStatusCode();
			using var stream = await data.Content.ReadAsStreamAsync();
			var maps = BSMapIO.ReadZip(stream);
			var timeToDownload = sw.Elapsed;

			sw.Restart();
			entry = new RamsesSong(request.MapId, RamsesVersion);
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
			var timeToProcess = sw.Elapsed;

			Log.Info("RaMSeS Key:{0} Download:{1} Process{2}", request.Key, timeToDownload, timeToProcess);

			await db.RamsesSongs.AddAsync(entry);
			await db.SaveChangesAsync();

			return entry;
		}

		private static long? GetMapIdFromKey(string key)
			=> long.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var mapId) ? (long?)mapId : null;

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
	}
}
