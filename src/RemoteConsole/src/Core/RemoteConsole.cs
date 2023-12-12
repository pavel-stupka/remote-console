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

        app.MapPost("/", async (context) =>
        {
            try
            {
                var result = await context.Request.BodyReader.ReadAsync();
                var bodyString = Encoding.UTF8.GetString(result.Buffer);
                var logRecord = ParseLogRecord(bodyString);
                PrintLogRecord(logRecord);
                context.Response.StatusCode = 204;
            } catch (Exception)
            {
                context.Response.StatusCode = 400;
            }
        });
        
        Console.WriteLine("running on port 5000");
        
        app.Run("http://+:5000");
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
        
        DateTime timestamp = DateTime.Parse(
            json.timestamp.ToString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.None
            );
        LogLevel logLevel = ParseLogLevel(json.logLevel.ToString());
        string tag = json.tag.ToString();
        string message = json.message.ToString();
        
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