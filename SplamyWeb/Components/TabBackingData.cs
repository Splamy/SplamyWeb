using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class TabBackingData
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly LocalDb db;
		private readonly ILiteCollection<TabStatsEntry> tabStatsTable;

		private const int MaxRunningBots = 10_000;
		private const int MaxDaysCalculation = 2; // Aim to send stats once per day. So 10 days should be the maximum for values calculation
		private static readonly TimeSpan MaxTotalUptime = TimeSpan.FromDays(MaxDaysCalculation);
		private const int MaxSongsPerFactory = 60 * 60 * 24 * MaxDaysCalculation;

		// Precaclulated stuff
		public uint Downloads { get; set; }
		public uint RunningInstances { get; set; }
		public uint RunningBots { get; set; }
		public uint PlaybackTime { get; set; }

		public TabBackingData(LocalDb db, TimerService timer)
		{
			this.db = db;
			tabStatsTable = db.TabStatsTable;
			timer.Register(UpdateAggregates);
		}

		public void Add(TabStatsData obj)
		{
			if (!VaidateTabStats(obj))
				return;

			Log.Info("Stats: {@stats}", obj);

			tabStatsTable.Insert(new TabStatsEntry
			{
				Time = DateTime.UtcNow,
				Data = obj,
			});
		}

		public TabStatsEntry Get()
		{
			return tabStatsTable.FindOne(Query.All(Query.Descending));
		}

		private bool VaidateTabStats(TabStatsData obj)
		{
			if (
				obj.Platform is null &&
				obj.Runtime is null &&
				obj.BotVersion is null)
				return false;

			if (obj.RunningBots > MaxRunningBots)
				return false;

			if (obj.TotalUptime > MaxTotalUptime)
				return false;

			if (obj.SongStats != null)
			{
				if (obj.SongStats.Count > 32)
					return false;

				if (obj.SongStats.Keys.Any(x => x.Length > 128))
					return false;

				if (obj.SongStats.Values.Any(x => x.PlayRequests > MaxSongsPerFactory))
					return false;
			}

			return true;
		}

		private Task UpdateAggregates()
		{
			Downloads = db.NightlyTable.Query()
				.Where(x => x.Project == "ts3ab")
				.ToEnumerable()
				.Select(x => x.DownloadCount)
				.Sum();

			var oneDayAgo = DateTime.Now - TimeSpan.FromDays(1);

			RunningInstances = (uint)tabStatsTable.Query()
				.Where(x => x.Time > oneDayAgo)
				.Count();

			RunningBots = tabStatsTable.Query()
				.Where(x => x.Time > oneDayAgo)
				.ToEnumerable()
				.Select(x => x.Data.RunningBots)
				.Sum();

			PlaybackTime = (uint)tabStatsTable.Query()
				.ToEnumerable()
				.SelectMany(x => x.Data.SongStats?.Values ?? Enumerable.Empty<TabStatsFactory>())
				.Select(x => x.Playtime)
				.Sum().TotalMinutes;

			return Task.CompletedTask;
		}
	}

	public class TabStatsEntry
	{
		public long Id { get; set; }
		public DateTime Time { get; set; }
		public TabStatsData Data { get; set; }

#pragma warning disable CS8618
		public TabStatsEntry() { }
#pragma warning restore CS8618
	}

	public class TabStatsData
	{
		// Meta
		public string? BotVersion { get; set; }
		public string? Platform { get; set; }
		public string? Runtime { get; set; }
		public uint? RunningBots { get; set; }
		public TimeSpan? TrackTime { get; set; }

		// StatsData
		public TimeSpan? TotalUptime { get; set; }
		public TimeSpan? BotsRuntime { get; set; }
		public Dictionary<string, TabStatsFactory>? SongStats { get; set; }

		public uint? CommandCalls { get; set; }
		///<summary>How many actually were started by a user (and not i.e. by event)</summary>
		public uint? CommandFromUser { get; set; }
		public uint? CommandFromApi { get; set; }
	}

	public class TabStatsFactory
	{
		public uint? PlayRequests { get; set; }
		public uint? PlaySucessful { get; set; }
		///<summary>How many actually were started by a user (and not i.e. from a playlist)</summary>
		public uint? PlayFromUser { get; set; }
		public uint? SearchRequests { get; set; }
		public TimeSpan? Playtime { get; set; }
	}
}
