using LiteDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TabController : ControllerBase
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly LiteCollection<TabStatsEntry> tabStatsTable;
		private readonly Dictionary<IPAddress, SpamCacheEntry> spamCache = new Dictionary<IPAddress, SpamCacheEntry>();

		private const int MaxIpRequests = 10_000;
		private static readonly TimeSpan ResetIpRequestsAfter = TimeSpan.FromHours(1);
		private const int MaxRunningBots = 10_000;
		private const int MaxDaysCalculation = 10; // Aim to send stats once per week. So 10 days should be the maximum for values calculation
		private static readonly TimeSpan MaxWorkTime = TimeSpan.FromDays(MaxDaysCalculation);
		private const int MaxSongsPerFactory = 60 * 60 * 24 * MaxDaysCalculation;

		public TabController(LocalDb db)
		{
			tabStatsTable = db.TabStatsTable;
		}

		[HttpPost("ping")]
		[Consumes("application/json")]
		public void PostPing([FromBody]TabStatsData obj)
		{
			lock (spamCache)
			{
				var reqIp = Request.HttpContext.Connection.RemoteIpAddress;
				if (!spamCache.TryGetValue(reqIp, out var spamCacheEntry) || spamCacheEntry.CreateTime + ResetIpRequestsAfter > DateTime.UtcNow)
				{
					spamCacheEntry = new SpamCacheEntry(DateTime.UtcNow);
					spamCache[reqIp] = spamCacheEntry;
				}
				if (spamCacheEntry.Count > MaxIpRequests)
				{
					return;
				}

				spamCacheEntry.Count++;
			}

			if (!VaidateTabStats(obj))
				return;
		}

		private bool VaidateTabStats(TabStatsData obj)
		{
			if (!(
				obj.Platform is null &&
				obj.Runtime is null &&
				obj.BotVersion is null))
				return false;

			if (obj.RunningBots > MaxRunningBots)
				return false;

			if (obj.RunTime > MaxWorkTime)
				return false;

			if (obj.SongStats != null)
			{
				if (obj.SongStats.Keys.Any(x => x.Length > 256))
					return false;

				if (obj.SongStats.Values.Any(x =>
					x.Requests > MaxSongsPerFactory
				))
					return false;
			}

			return true;
		}

		[HttpGet("check")]
		[Produces("application/json")]
		[Authorize(AuthenticationSchemes = AuthScheme)]
		public IActionResult GetCheck()
		{
			var entry = tabStatsTable.FindOne(Query.All(Query.Descending));
			return Ok(entry);
		}
	}

	public class SpamCacheEntry
	{
		public DateTime CreateTime { get; }
		public int Count { get; set; }

		public SpamCacheEntry(DateTime createTime)
		{
			CreateTime = createTime;
			Count = 0;
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
		public TimeSpan? RunTime { get; set; }
		public TimeSpan? BotsRunTime { get; set; }
		public Dictionary<string, TabStatsFactory>? SongStats { get; set; }
	}

	public class TabStatsFactory
	{
		public int? Requests { get; set; }
		public int? Loaded { get; set; }
		///<summary>How many actually were started by a user (and not i.e. from a playlist)</summary>
		public int? FromUser { get; set; }
		public TimeSpan? Playtime { get; set; }
	}
}
