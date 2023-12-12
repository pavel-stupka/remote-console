namespace RemoteConsole.Core;
public record struct LogRecord(DateTime Timestamp, LogLevel LogLevel, string Tag, string Message);
