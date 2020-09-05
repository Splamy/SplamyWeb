using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SplamyWeb.Db;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class TabBackingData
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly IServiceScopeFactory scopeFactory;
		private readonly IMapper mapper;
		private const int MaxRunningBots = 10_000;
		private const int MaxDaysCalculation = 2; // Aim to send stats once per day. So 10 days should be the maximum for values calculation
		private static readonly TimeSpan MaxTotalUptime = TimeSpan.FromDays(MaxDaysCalculation);
		private const int MaxSongsPerFactory = 60 * 60 * 24 * MaxDaysCalculation;

		// Precaclulated stuff
		public uint Downloads { get; set; }
		public uint RunningInstances { get; set; }
		public uint RunningBots { get; set; }
		public TimeSpan PlaybackTime { get; set; }
		public CachedDayStats[] CachedDayStats { get; set; } = Array.Empty<CachedDayStats>();

		public TabBackingData(IServiceScopeFactory scopeFactory, IMapper mapper, TimerService timer)
		{
			this.scopeFactory = scopeFactory;
			this.mapper = mapper;
			timer.Register(UpdateAggregates);
		}

		public async Task Add(TabStatsData obj)
		{
			if (!VaidateTabStats(obj))
				return;

			Log.Info("Stats: {@stats}", obj);

			var dto = mapper.Map<TabStatsData, TabStatsEntryDto>(obj);
			dto.Time = DateTime.UtcNow;

			using var scope = scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
			await db.TabStatsTable.AddAsync(dto);
			await db.SaveChangesAsync();
		}

		private bool VaidateTabStats(TabStatsData obj)
		{
			if (
				obj.Platform is null &&
				obj.Runtime is null &&
				obj.BotVersion is null)
				return false;

			if (obj.BotVersion == "0.11.0-alpha.50/develop/96162298" || obj.TotalUptime < TimeSpan.FromMinutes(3)) // TODO: Temporary block against wrong configuration
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

		private async Task UpdateAggregates()
		{
			using var scope = scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

			//Downloads = db.NightlyTable.Query()
			//	.Where(x => x.Project == "ts3ab")
			//	.ToEnumerable()
			//	.Select(x => x.DownloadCount)
			//	.Sum();

			//var oneDayAgo = DateTime.Now - TimeSpan.FromDays(1);

			//RunningInstances = (uint)tabStatsTable.Query()
			//	.Where(x => x.Time > oneDayAgo)
			//	.Count();

			//RunningBots = tabStatsTable.Query()
			//	.Where(x => x.Time > oneDayAgo)
			//	.ToEnumerable()
			//	.Select(x => x.Data.RunningBots)
			//	.Sum();

			//PlaybackTime = tabStatsTable.Query()
			//	.ToEnumerable()
			//	.SelectMany(x => x.Data.SongStats?.Values ?? Enumerable.Empty<TabStatsFactory>())
			//	.Select(x => x.Playtime)
			//	.Sum();

			var beforeBug = new DateTime(2020, 3, 17);

			//var x = (
			//	from entry in db.TabStatsTable
			//	where entry.Time >= beforeBug
			//	from fact in entry.SongStats
			//	group new { entry, fact } by entry.Time.Date into agg
			//	orderby agg.Key
			//	select new
			//	{
			//		Date = agg.Key,
			//		RunningBots = agg.Sum(x => x.entry.RunningBots),
			//		RunningInstances = agg.Count(),
			//		PlaybackTime = agg.Sum(x => x.fact.Playtime.TotalSeconds)
			//	});
			CachedDayStats = await db.Set<CachedDayStats>().FromSqlRaw(
@"SELECT * FROM
(
	SELECT DATE_TRUNC('day', ""Time"") AS Date, SUM(""RunningBots"") AS RunningBots, COUNT(*) as RunningInstances
	FROM tabstats_entry
	GROUP BY Date
) e
INNER JOIN
(
	SELECT DATE_TRUNC('day', ""Time"") AS Date, SUM(""Playtime"") AS PlaybackTime
	FROM tabstats_entry
	JOIN tabstats_factory ON ""TabStatsId"" = ""Id""
	GROUP BY Date
) f
USING(Date)
ORDER BY Date").ToArrayAsync();

		}
	}

	public class CachedDayStats
	{
		public DateTime Date { get; set; }
		public uint RunningInstances { get; set; }
		public uint RunningBots { get; set; }
		public TimeSpan PlaybackTime { get; set; }
	}
}
