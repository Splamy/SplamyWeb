using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class SpamBackingData
{
	private readonly Dictionary<IPAddress, SpamCacheEntry> spamCache = [];

	private const int MaxIpRequests = 1_000;
	private static readonly TimeSpan ResetIpRequestsAfter = TimeSpan.FromHours(1);
	private readonly TimeProvider timeProvider;

	public SpamBackingData(ITimerService timer, TimeProvider timeProvider)
	{
		timer.Register(CleanIpTables);
		this.timeProvider = timeProvider;
	}

	public bool Check(IPAddress reqIp)
	{
		var now = timeProvider.GetUtcNow().DateTime;
		lock (spamCache)
		{
			ref var spamCacheEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(spamCache, reqIp, out var exists);

			//var reqIp = Request.HttpContext.Connection.RemoteIpAddress;
			if (!exists || spamCacheEntry.CreateTime < now - ResetIpRequestsAfter)
			{
				spamCacheEntry.CreateTime = now;
				spamCacheEntry.Count = 0;
			}
			else if (spamCacheEntry.Count >= MaxIpRequests)
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

	struct SpamCacheEntry
	{
		public DateTime CreateTime { get; set; }
		public int Count { get; set; }
	}
}
