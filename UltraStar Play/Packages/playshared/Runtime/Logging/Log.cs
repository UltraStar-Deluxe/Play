using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using UnityEngine;

public static class Log
{
    private const int LogTextMaxLengthInChars = 100000;

    public static readonly string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{StackTrace}";
    public static readonly string logFileFolder = $"{Application.persistentDataPath}/Logs";
    public static readonly string logFilePath = $"{logFileFolder}/{Application.productName}.log";

    public static Serilog.ILogger Logger { get; private set; }

    private static LogHistorySink logHistorySink;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        logHistorySink = new();
        Logger = CreateLogger();
    }

    public static List<LogEvent> GetLogHistory()
    {
        return logHistorySink.LogHistory;
    }

    private static Serilog.ILogger CreateLogger()
    {
        if (!Directory.Exists(logFileFolder))
        {
            Directory.CreateDirectory(logFileFolder);
        }
        int fileSizeLimitBytes = 250 * 1000 * 1000; // 250 MB
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.With<UnityStackTraceEnricher>()
            .WriteTo.Sink(new UnityLogEventSink())
            .WriteTo.Sink(logHistorySink)
            .WriteTo.File(
                logFilePath, // path
                LogEventLevel.Verbose, // restrictedToMinimumLevel
                outputTemplate, // outputTemplate
                null, // formatProvider
                fileSizeLimitBytes, // fileSizeLimitBytes
                null, // levelSwitch
                false, // buffered
                false, // shared
                null, // flushToDiskInterval
                RollingInterval.Day, // rollingInterval
                false, // rollOnFileSizeLimit
                5, // retainedFileCountLimit
                System.Text.Encoding.UTF8, // Encoding
                null); // FileLifecycleHooks

        Serilog.ILogger logger = loggerConfiguration.CreateLogger();
        logger.Information("===== Initialized Serilog Logger =====");
        return logger;
    }

    public static void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (logString.EndsWith(UnityLogEventSink.unityLogEventSinkMarker))
        {
            // Has already been logged to the Unity Console.
            return;
        }

        Serilog.ILogger loggerWithContext = Logger.ForContext(UnityLogEventSink.skipUnityLogEventSinkPropertyName, true);

        switch (type)
        {
            case LogType.Warning:
                loggerWithContext.Warning(logString);
                break;
            case LogType.Assert:
            case LogType.Error:
            case LogType.Exception:
                loggerWithContext.Error(logString + "\n" + stackTrace);
                break;
            default:
                loggerWithContext.Information(logString);
                break;
        }
    }

    public static string GetLogText(LogEventLevel logEventLevel)
    {
        MessageTemplateTextFormatter textFormatter = new(Log.outputTemplate);
        List<string> logLines = GetLogHistory()
            .Where(logEvent => (int)logEvent.Level >= (int)logEventLevel)
            .Select(logEvent =>
            {
                StringWriter stringWriter = new();
                textFormatter.Format(logEvent, stringWriter);
                string logLine = stringWriter.ToString();
                // Workaround for Unity TextField interpreting backslash for special characters.
                return logLine.Replace("\\", "/");
            })
            .ToList();

        string logText = logLines.IsNullOrEmpty()
            ? "(no log messages)"
            : logLines.JoinWith("");
        if (logText.Length > LogTextMaxLengthInChars)
        {
            string prefix = "...\n";
            logText = prefix + logText.Substring(logText.Length - (LogTextMaxLengthInChars - prefix.Length));
        }
        return logText;
    }
}
