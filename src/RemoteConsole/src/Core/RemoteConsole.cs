using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace RemoteConsole.Core;

public static class RemoteConsole
{
    public static void Execute(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        
        var app = builder.Build();
        
        var port = GetPort(args);

        app.MapPost("/", async (context) =>
        {
            try
            {
                var result = await context.Request.BodyReader.ReadAsync();
                var bodyString = Encoding.UTF8.GetString(result.Buffer);
                var logRecord = ParseLogRecord(bodyString);
                PrintLogRecord(logRecord);
                context.Response.StatusCode = 204;
            } catch (Exception ex)
            {
                Console.WriteLine("Error parsing log record: " + ex.Message);
                context.Response.StatusCode = 400;
            }
        });
        
        Console.WriteLine($"listening on port {port}");
        
        app.Run($"http://+:{port}");
    }

    private static int GetPort(string[] args)
    {   
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" || args[i] == "-p")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                {
                    return port;
                }
            }
        }
        return 5000;
    }
    
    private static void PrintLogRecord(LogRecord logRecord)
    {
        var timestamp = logRecord.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        
        Console.ForegroundColor = logRecord.LogLevel switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.DarkCyan,
            LogLevel.Info => ConsoleColor.DarkYellow,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.White,
            _ => ConsoleColor.White
        };

        if (logRecord.LogLevel == LogLevel.Fatal)
        {
            Console.BackgroundColor = ConsoleColor.Red;
        }
        
        Console.WriteLine($"{timestamp} [{logRecord.LogLevel.ToString().ToUpper()[0]}] <{logRecord.Tag}>: {logRecord.Message}");
        
        Console.ResetColor();
    }

    private static LogRecord ParseLogRecord(string jsonLogRecord)
    {
        dynamic json = JsonConvert.DeserializeObject(jsonLogRecord)!;
        
        DateTime timestamp = json.timestamp != null ? DateTime.Parse(json.timestamp.ToString(), null, DateTimeStyles.RoundtripKind) : DateTime.Now;
        LogLevel logLevel = json.logLevel != null ? ParseLogLevel(json.logLevel.ToString()) : LogLevel.Debug;
        string tag = json.tag != null ? json.tag.ToString() : "unknown";
        string message = json.message != null ? json.message.ToString() : "no message";
        
        return new LogRecord(timestamp, logLevel, tag, message);
    }

    private static LogLevel ParseLogLevel(string logLevel)
    {
        return logLevel switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "fatal" => LogLevel.Fatal,
            _ => LogLevel.Info
        };
    }
}