using AutoMapper;
using NLog;
using NLog.Layouts;
using System;

namespace SplamyWeb
{
	public static class ServerLog
	{
		public delegate void LogEvent(LogEventInfo ev);
		public static event LogEvent? OnLog;

		public static readonly Layout DefaultLayout
			= Layout.FromString("${pad:padding=5:inner=${level:uppercase=true}} ${message} ${exception:format=ToString}");

		public static readonly NLog.Targets.MemoryTarget NLogMemory = new()
		{
			Layout = DefaultLayout,
		};

		public static readonly NLog.Targets.MethodCallTarget NLogEvent = new("MethodLogger",
			(ev, param) => { OnLog?.Invoke(ev); }
		);
	}

	public class ServerLogEntry : IComparable<ServerLogEntry>
	{
		public int SequenceID { get; init; }
		public DateTime TimeStamp { get; init; }
		public LogLevel Level { get; init; }
		public string LoggerName { get; init; }
		public string FormattedMessage { get; init; }

		public ServerLogEntry(int sequenceID, DateTime timeStamp, LogLevel level, string loggerName, string formattedMessage)
		{
			SequenceID = sequenceID;
			TimeStamp = timeStamp;
			Level = level;
			LoggerName = loggerName;
			FormattedMessage = formattedMessage;
		}

		public static ServerLogEntry Comparer(int seq) => new(seq, default, default!, default!, default!);

		public int CompareTo(ServerLogEntry? other) => other != null ? SequenceID - other.SequenceID : 0;
	}

	public class ServerLogEntryProfile : Profile
	{
		public ServerLogEntryProfile()
		{
			CreateMap<LogEventInfo, ServerLogEntry>(MemberList.Destination);
		}
	}
}
