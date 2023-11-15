using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Riok.Mapperly.Abstractions;

namespace SplamyWeb;

public static class ServerLog
{
	public delegate void LogEvent(LogEventInfo ev);
	public static event LogEvent? OnLog;

	public static readonly Layout DefaultLayout
		= Layout.FromString("${pad:padding=5:inner=${level:uppercase=true}} ${message} ${exception:format=ToString}");

	public static readonly MemoryTarget NLogMemory = new()
	{
		Layout = DefaultLayout,
	};

	public static readonly MethodCallTarget NLogEvent = new("MethodLogger",
		(ev, param) => { OnLog?.Invoke(ev); }
	);

	public static void ConfigueNLog()
	{
		var config = new LoggingConfiguration();
		var consoleTarget = new ConsoleTarget { Layout = DefaultLayout };
		var nullTarget = new NullTarget();
		config.AddRule(LogLevel.Trace, LogLevel.Off, nullTarget, "SplamyWeb.Components.BasicAuthenticationHandler", final: true);
		config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "SplamyWeb.*");
		config.AddRule(LogLevel.Debug, LogLevel.Fatal, NLogMemory, "SplamyWeb.*");
		config.AddRule(LogLevel.Debug, LogLevel.Fatal, NLogEvent, "SplamyWeb.*");

		LogManager.Configuration = config;
	}
}

#pragma warning disable CA1036 // Override methods on comparable types
public class ServerLogEntry(int sequenceID, DateTime timeStamp, LogLevel level, string loggerName, string formattedMessage)
	: IComparable<ServerLogEntry>
{
	public int SequenceID { get; init; } = sequenceID;
	public DateTime TimeStamp { get; init; } = timeStamp;
	public LogLevel Level { get; init; } = level;
	public string LoggerName { get; init; } = loggerName;
	public string FormattedMessage { get; init; } = formattedMessage;

	public static ServerLogEntry Comparer(int seq) => new(seq, default, default!, default!, default!);

	public int CompareTo(ServerLogEntry? other) => other != null ? SequenceID - other.SequenceID : 0;
}
#pragma warning restore CA1036 // Override methods on comparable types

[Mapper]
public static partial class ServerLogMapper
{
#pragma warning disable RMG020
	public static partial ServerLogEntry ToEntry(LogEventInfo ev);
#pragma warning restore RMG020
}
