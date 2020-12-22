using System;
using System.IO;
using Serilog;
using Serilog.Events;
using UnityEngine;

public static class Log
{
    private static readonly string logFileFolder = Application.persistentDataPath + "/Logs";
    private static readonly string logFilePath = logFileFolder + "/UltraStarPlay.log";

    public static Serilog.ILogger Logger { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Logger = CreateLogger();
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
            .WriteTo.File(
                logFilePath, // path
                LogEventLevel.Verbose, // restrictedToMinimumLevel
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{StackTrace}", // outputTemplate
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
            case LogType.Log:
                loggerWithContext.Information(logString);
                break;
            case LogType.Warning:
                loggerWithContext.Warning(logString);
                break;
            case LogType.Assert:
            case LogType.Error:
            case LogType.Exception:
                loggerWithContext.Error(logString + stackTrace);
                break;
        }
    }
}
