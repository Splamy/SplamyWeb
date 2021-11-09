using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class SpamBackingData
{
	private readonly Dictionary<IPAddress, SpamCacheEntry> spamCache = new();

	private const int MaxIpRequests = 1_000;
	private static readonly TimeSpan ResetIpRequestsAfter = TimeSpan.FromHours(1);

	public SpamBackingData(TimerService timer)
	{
		timer.Register(CleanIpTables);
	}

	public bool Check(IPAddress reqIp)
	{
		lock (spamCache)
		{
			//var reqIp = Request.HttpContext.Connection.RemoteIpAddress;
			if (!spamCache.TryGetValue(reqIp, out var spamCacheEntry) || spamCacheEntry.CreateTime < DateTime.UtcNow - ResetIpRequestsAfter)
			{
				spamCacheEntry = new SpamCacheEntry(DateTime.UtcNow);
				spamCache[reqIp] = spamCacheEntry;
			}
			if (spamCacheEntry.Count > MaxIpRequests)
			{
				return false;
			}

			spamCacheEntry.Count++;
		}
		return true;
	}

	private async Task CleanIpTables()
	{
		await Task.Yield();
		lock (spamCache)
		{
			var nowTimeout = DateTime.UtcNow - ResetIpRequestsAfter;
			var kvpList = spamCache.ToArray();
			foreach (var kvp in kvpList)
			{
				if (kvp.Value.CreateTime < nowTimeout)
				{
					spamCache.Remove(kvp.Key);
				}
			}
		}
	}

	class SpamCacheEntry
	{
		public DateTime CreateTime { get; }
		public int Count { get; set; }

		public SpamCacheEntry(DateTime createTime)
		{
			CreateTime = createTime;
			Count = 0;
		}
	}
}
