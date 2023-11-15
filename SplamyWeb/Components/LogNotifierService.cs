using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NLog;
using System.Linq;

namespace SplamyWeb.Components;

public class LogNotifierService
{
	private readonly object listLock = new();
	private readonly List<ServerLogEntry> logHistory = [];
	private readonly IHubContext<LogNotifier> hub;

	public LogNotifierService(IHubContext<LogNotifier> hub)
	{
		this.hub = hub;
		ServerLog.OnLog += NotifyLog;
	}

	private async void NotifyLog(LogEventInfo ev)
	{
		var entry = ServerLogMapper.ToEntry(ev);
		lock (listLock)
		{
			logHistory.Add(entry);
			if (logHistory.Count >= 1100)
				logHistory.RemoveRange(0, 100);
		}

		await hub.Clients.All.SendAsync("Log", entry);
	}

	public ServerLogEntry[] GetTop()
	{
		lock (listLock)
		{
			return logHistory.TakeLast(50).ToArray();
		}
	}

	public ServerLogEntry[] GetLog(int fromId, int length)
	{
		lock (listLock)
		{
			var pos = logHistory.BinarySearch(ServerLogEntry.Comparer(fromId));
			var select = pos >= 0 ? pos : ~pos - 1;
			return logHistory.Skip(select).Take(length).ToArray();
		}
	}
}

[Authorize]
public class LogNotifier(LogNotifierService logNotifierService) : Hub
{
	public ServerLogEntry[] GetTop() => logNotifierService.GetTop();

	public ServerLogEntry[] GetLog(int fromId, int length) => logNotifierService.GetLog(fromId, length);
}
