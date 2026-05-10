using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class SpamBackingData
{
	private readonly Lock _spamLock = new();
	private readonly Dictionary<IPAddress, SpamCacheEntry> _spamCache = [];

	private const int MaxIpRequests = 1_000;
	private static readonly TimeSpan ResetIpRequestsAfter = TimeSpan.FromHours(1);
	private readonly TimeProvider _timeProvider;

	public SpamBackingData(ITimerService timer, TimeProvider timeProvider)
	{
		timer.Register(CleanIpTables);
		_timeProvider = timeProvider;
	}

	public bool Check(IPAddress reqIp)
	{
		var now = _timeProvider.GetUtcNow().DateTime;
		lock (_spamLock)
		{
			ref var spamCacheEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(_spamCache, reqIp, out var exists);

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
		lock (_spamLock)
		{
			var nowTimeout = DateTime.UtcNow - ResetIpRequestsAfter;
			var kvpList = _spamCache.ToArray();
			foreach (var kvp in kvpList)
			{
				if (kvp.Value.CreateTime < nowTimeout)
				{
					_spamCache.Remove(kvp.Key);
				}
			}
		}
	}

	private struct SpamCacheEntry
	{
		public DateTime CreateTime { get; set; }
		public int Count { get; set; }
	}
}
