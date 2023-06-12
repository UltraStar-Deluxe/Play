using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using UniRx;
using UnityEngine;
using ILogger = Serilog.ILogger;
using Object = UnityEngine.Object;

public static class Log
{
    private const int LogTextMaxLengthInChars = 100000;

    public static readonly string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{StackTrace}";
    public static readonly string logFileFolder = $"{Application.persistentDataPath}/Logs";
    public static readonly string logFilePath = $"{logFileFolder}/{Application.productName}.log";

    private static ILogger Logger { get; set; }

    private static LogHistorySink logHistorySink;

    private static ILogHandler defaultUnityLogHandler;
    private static readonly ILogHandler customUnityLogHandler = new CustomUnityLogHandler();

    private static readonly Subject<LogEvent> logEventStream = new();
    public static IObservable<LogEvent> LogEventStream => logEventStream;

    public static bool IsUsingDefaultUnityLogHandler => defaultUnityLogHandler == null 
                                                        || Debug.unityLogger.logHandler == defaultUnityLogHandler 
                                                        || Application.isEditor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        logHistorySink = new();
        Logger = CreateLogger();
        Logger.Information("===== Initialized Serilog Logger =====");
        UseCustomUnityLogHandler();
    }

    public static List<LogEvent> GetLogHistory()
    {
        return logHistorySink.LogHistory;
    }

    private static ILogger CreateLogger()
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
                Encoding.UTF8, // Encoding
                null) // FileLifecycleHooks
            .WriteTo.Sink(new LogEventStreamSink());

        ILogger logger = loggerConfiguration.CreateLogger();
        return logger;
    }

    public static string GetLogText(LogEventLevel logEventLevel)
    {
        MessageTemplateTextFormatter textFormatter = new(outputTemplate);
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

    public static LogType GetUnityLogType(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Verbose:
            case LogEventLevel.Debug:
            case LogEventLevel.Information:
                return LogType.Log;
            case LogEventLevel.Warning:
                return LogType.Warning;
            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
                return logEvent.Exception != null ? LogType.Exception : LogType.Error;
            default:
                Debug.LogError("Unknown LogLevel" + logEvent.Level);
                return LogType.Log;
        }
    }
    
    private static void UseCustomUnityLogHandler()
    {
        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = Debug.unityLogger.logHandler;
        }

        if (Debug.unityLogger.logHandler == customUnityLogHandler)
        {
            return;
        }

        Debug.unityLogger.logHandler = customUnityLogHandler;
        Debug.Log("===== Using Custom Unity Log Handler =====");
    }
    
    private static void UseDefaultUnityLogHandler()
    {
        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = Debug.unityLogger.logHandler;
        }
        
        if (Debug.unityLogger.logHandler == defaultUnityLogHandler)
        {
            return;
        }

        Debug.unityLogger.logHandler = defaultUnityLogHandler;
        Debug.Log("===== Using Default Unity Log Handler =====");
    }
    
    private class CustomUnityLogHandler : ILogHandler
    {
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (Logger == null)
            {
                defaultUnityLogHandler?.LogFormat(logType, context, format, args);
                return;
            }
            
            switch (logType)
            {
                case LogType.Log:
                    Logger.Information(GetLogMessage(context, format, args));
                    break;
                case LogType.Warning:
                    Logger.Warning(GetLogMessage(context, format, args));
                    break;
                case LogType.Error:
                    Logger.Error(GetLogMessage(context, format, args));
                    break;
                case LogType.Exception:
                    Logger.Error(GetLogMessage(context, format, args));
                    break;
                case LogType.Assert:
                    Logger.Fatal(GetLogMessage(context, format, args));
                    break;
                default:
                    Logger.Information(GetLogMessage(context, format, args));
                    break;
            }

            if (Application.isEditor)
            {
                // Forward to UnityEditor's console via defaultUnityLogHandler.
                // This must not be logged again with the Serilog Logger to avoid an infinite loop.
                defaultUnityLogHandler?.LogFormat(logType, context, format, args);
            }
        }

        public void LogException(Exception exception, Object context)
        {
            if (Logger == null)
            {
                defaultUnityLogHandler?.LogException(exception, context);
                return;
            }
            
            Logger.Error(exception, GetLogMessage(context, "{0}", exception.Message));
        }

        private string GetLogMessage(Object context, string format, params object[] args)
        {
            if (context == null)
            {
                return string.Format(format, args);
            }

            return string.Format($"[{context.name}] {format}", args);;
        }
    }
    
    private class LogEventStreamSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            logEventStream.OnNext(logEvent);
        }
    }
}
