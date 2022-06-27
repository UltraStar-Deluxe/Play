using System.Collections.Generic;
using System.IO;
using Serilog;
using Serilog.Events;
using UnityEngine;

public static class Log
{
    public static readonly string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{StackTrace}";
    private static readonly string logFileFolder = $"{Application.persistentDataPath}/Logs";
    public static readonly string logFilePath = $"{logFileFolder}/{Application.productName}.log";

    public static Serilog.ILogger Logger { get; private set; }

    private static readonly LogHistorySink logHistorySink = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
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
}
