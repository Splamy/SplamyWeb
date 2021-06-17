using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace SplamyWeb.Components
{
	public class LogNotifierService
	{
		private readonly object listLock = new();
		private readonly List<ServerLogEntry> logHistory = new();
		private readonly IHubContext<LogNotifier> hub;
		private readonly IMapper mapper;

		public LogNotifierService(IHubContext<LogNotifier> hub, IMapper mapper)
		{
			this.hub = hub;
			this.mapper = mapper;
			ServerLog.OnLog += NotifyLog;
		}

		private async void NotifyLog(LogEventInfo ev)
		{
			var entry = mapper.Map<ServerLogEntry>(ev);
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
				return logHistory.Take(50).ToArray();
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
	public class LogNotifier : Hub
	{
		private readonly LogNotifierService logNotifierService;

		public LogNotifier(LogNotifierService logNotifierService)
		{
			this.logNotifierService = logNotifierService;
		}

		public ServerLogEntry[] GetTop() => logNotifierService.GetTop();

		public ServerLogEntry[] GetLog(int fromId, int length) => logNotifierService.GetLog(fromId, length);
	}
}
