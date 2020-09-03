using LiteDB;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RateMapSeveritySaber;
using SplamyWeb.Db;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
		private readonly BufferBlock<ProcessEntry> _bufferBlock = new BufferBlock<ProcessEntry>();
		private readonly LocalDb db;
		private readonly Task processTask;

		private readonly string RamsesVersion;

		public RamsesBackingData(LocalDb db)
		{
			this.db = db;
			var ver = typeof(Analyzer).Assembly.GetName().Version!;
			RamsesVersion = $"{ver.Major}.{ver.Minor}";
			processTask = Process();
		}

		private async Task Process()
		{
			while (true)
			{
				var req = await _bufferBlock.ReceiveAsync();
				RamsesEntry? res;
				try
				{
					res = await GetInternal(req);
				}
				catch (Exception ex)
				{
					Log.Warn(ex, "Failed to process song: {0}", ex.Message);

					res = new RamsesEntry(req.MapId, RamsesVersion);
				}
				req.Task.SetResult(res);
			}
		}

		public async Task<RamsesEntry?> Get(string key)
		{
			var mapId = GetMapIdFromKey(key);
			if (mapId == null) return null;
			var req = new ProcessEntry(key, mapId.Value);
			await _bufferBlock.SendAsync(req);
			return await req.Task.Task;
		}

		private async Task<RamsesEntry?> GetInternal(ProcessEntry request)
		{
			var entry = (from entries in db.Context.RamsesEntries
						 where entries.Id == request.MapId
						 select entries)
						 .Include(e => e.Maps)
						 .FirstOrDefault();

			//var entry = ramsesTable.FindById(key);
			if (entry != null && entry.Version == RamsesVersion)
				return entry;

			var sw = Stopwatch.StartNew();
			using var data = await Util.httpClient.GetAsync($"https://beatsaver.com/api/download/key/{request.Key}");
			data.EnsureSuccessStatusCode();
			using var stream = await data.Content.ReadAsStreamAsync();
			var maps = BSMapIO.ReadZip(stream);
			var timeToDownload = sw.Elapsed;

			sw.Restart();
			entry = new RamsesEntry(request.MapId, RamsesVersion);
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

			await db.Context.RamsesEntries.AddAsync(entry);
			await db.Context.SaveChangesAsync();

			return entry;
		}

		private static long? GetMapIdFromKey(string key)
			=> long.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var mapId) ? (long?)mapId : null;

		class ProcessEntry
		{
			public string Key { get; }
			public long MapId { get; }
			public TaskCompletionSource<RamsesEntry?> Task { get; }

			public ProcessEntry(string key, long mapId)
			{
				Key = key;
				MapId = mapId;
				Task = new TaskCompletionSource<RamsesEntry?>();
			}
		}
	}
}
