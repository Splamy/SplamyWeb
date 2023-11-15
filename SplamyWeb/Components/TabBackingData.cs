using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SplamyWeb.Db;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class TabBackingData
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private readonly IServiceScopeFactory scopeFactory;
	private const int MaxRunningBots = 10_000;
	private const int MaxDaysCalculation = 2; // Aim to send stats once per day. So 10 days should be the maximum for values calculation
	private static readonly TimeSpan MaxTotalUptime = TimeSpan.FromDays(MaxDaysCalculation);
	private const int MaxSongsPerFactory = 60 * 60 * 24 * MaxDaysCalculation;

	// Precaclulated stuff
	public uint Downloads { get; set; }
	public uint RunningInstances { get; set; }
	public uint RunningBots { get; set; }
	public TimeSpan PlaybackTime { get; set; }
	public CachedDayStats[] CachedDayStats { get; set; } = [];

	public TabBackingData(IServiceScopeFactory scopeFactory, TimerService timer)
	{
		this.scopeFactory = scopeFactory;
		timer.Register(UpdateAggregates);
	}

	public async Task Add(TabStatsData obj)
	{
		if (!VaidateTabStats(obj))
			return;

		Log.Info("Stats: {@stats}", obj);

		var dto = TabStatsMapper.ToDto(obj);
		dto.Time = DateTime.UtcNow;

		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
		await db.TabStatsPings.AddAsync(dto);
		await db.SaveChangesAsync();
	}

	private static bool VaidateTabStats(TabStatsData obj)
	{
		if (
			obj.Platform is null &&
			obj.Runtime is null &&
			obj.BotVersion is null)
			return false;

		// This version messed up the stats caclulation
		if (obj.BotVersion == "0.11.0-alpha.50/develop/96162298" || obj.TotalUptime < TimeSpan.FromMinutes(3))
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

	public async Task UpdateAggregates()
	{
		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		Downloads = (uint)await db.NightlyBuilds.AsNoTracking()
			.Where(x => x.Project == "ts3ab")
			.Select(x => x.DownloadCount)
			.SumAsync();

		var oneDayAgo = (DateTime.UtcNow - TimeSpan.FromDays(1)).ToUniversalTime();

		RunningInstances = (uint)await db.TabStatsPings.AsNoTracking()
			.Where(x => x.Time > oneDayAgo)
			.CountAsync();

		RunningBots = (uint)await db.TabStatsPings.AsNoTracking()
			.Where(x => x.Time > oneDayAgo)
			.Select(x => x.RunningBots)
			.SumAsync();

		PlaybackTime = (await db.Set<PlaytimeDto>().FromSqlRaw(
@"SELECT SUM(""Playtime"") as ""Playtime""
FROM tabstats_factory").FirstOrDefaultAsync())?.Playtime ?? TimeSpan.Zero;

		CachedDayStats = await db.Set<CachedDayStats>().FromSqlRaw(
@"SELECT DATE_TRUNC('day', ""Time"") AS Date, SUM(""RunningBots"") AS RunningBots, COUNT(*) as RunningInstances, COALESCE(SUM(f.""PlaybackTime""), INTERVAL '0') as PlaybackTime
FROM tabstats_entry
LEFT OUTER JOIN
(
	SELECT SUM(""Playtime"") AS ""PlaybackTime"", tf.""TabStatsId""
	FROM tabstats_factory tf
	GROUP BY tf.""TabStatsId""
) f
ON f.""TabStatsId"" = tabstats_entry.""Id""
GROUP BY Date
ORDER BY Date").ToArrayAsync();
	}
}

[Keyless]
public class CachedDayStats
{
	public DateTime Date { get; set; }
	public uint RunningInstances { get; set; }
	public uint RunningBots { get; set; }
	public TimeSpan PlaybackTime { get; set; }
}

[Keyless]
public class PlaytimeDto
{
	public TimeSpan? Playtime { get; set; }
}
