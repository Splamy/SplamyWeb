using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NLog;
using System.Linq;
using System.Threading;

namespace SplamyWeb.Components;

public class LogNotifierService
{
	private readonly Lock _listLock = new();
	private readonly List<ServerLogEntry> _logHistory = [];
	private readonly IHubContext<LogNotifier> _hub;

	public LogNotifierService(IHubContext<LogNotifier> hub)
	{
		_hub = hub;
		ServerLog.OnLog += NotifyLog;
	}

	private async void NotifyLog(LogEventInfo ev)
	{
		var entry = ServerLogMapper.ToEntry(ev);
		lock (_listLock)
		{
			_logHistory.Add(entry);
			if (_logHistory.Count >= 1100)
				_logHistory.RemoveRange(0, 100);
		}

		await _hub.Clients.All.SendAsync("Log", entry);
	}

	public ServerLogEntry[] GetTop()
	{
		lock (_listLock)
		{
			return _logHistory.TakeLast(50).ToArray();
		}
	}

	public ServerLogEntry[] GetLog(int fromId, int length)
	{
		lock (_listLock)
		{
			var pos = _logHistory.BinarySearch(ServerLogEntry.Comparer(fromId));
			var select = pos >= 0 ? pos : ~pos - 1;
			return _logHistory.Skip(select).Take(length).ToArray();
		}
	}
}

[Authorize]
public class LogNotifier(LogNotifierService logNotifierService) : Hub
{
	public ServerLogEntry[] GetTop() => logNotifierService.GetTop();

	public ServerLogEntry[] GetLog(int fromId, int length) => logNotifierService.GetLog(fromId, length);
}
