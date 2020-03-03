using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplamyWeb.Components
{
	public class TabBackingData
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly LiteCollection<TabStatsEntry> tabStatsTable;

		private const int MaxRunningBots = 10_000;
		private const int MaxDaysCalculation = 10; // Aim to send stats once per week. So 10 days should be the maximum for values calculation
		private static readonly TimeSpan MaxTotalUptime = TimeSpan.FromDays(MaxDaysCalculation);
		private const int MaxSongsPerFactory = 60 * 60 * 24 * MaxDaysCalculation;

		public TabBackingData(LocalDb db)
		{
			tabStatsTable = db.TabStatsTable;
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

				if (obj.SongStats.Values.Any(x => x.Requests > MaxSongsPerFactory))
					return false;
			}

			return true;
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
		public string? Platform { get; set; }
		public string? Runtime { get; set; }
		public string? BotVersion { get; set; }
		public int? RunningBots { get; set; }
		public TimeSpan? TrackTime { get; set; } // ?

		public TimeSpan? TotalUptime { get; set; }
		public TimeSpan? BotsRunTime { get; set; } // !
		public Dictionary<string, TabStatsFactory>? SongStats { get; set; }
		public TabStatsCommands? Commands { get; set; }
	}

	public class TabStatsFactory
	{
		public int? Requests { get; set; }
		public int? Loaded { get; set; }
		///<summary>How many actually were started by a user (and not i.e. from a playlist)</summary>
		public int? FromUser { get; set; }
		public TimeSpan? Playtime { get; set; }
	}

	public class TabStatsCommands
	{
		public uint CommandCalls { get; set; } = 0;
		///<summary>How many actually were started by a user (and not i.e. by event)</summary>
		public uint FromUser { get; set; } = 0;
	}
}
