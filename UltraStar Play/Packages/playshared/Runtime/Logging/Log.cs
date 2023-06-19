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
    
    private static readonly MessageTemplateTextFormatter textFormatter = new(outputTemplate);

    private static LoggingLevelSwitch loggingLevelSwitch;
    private static ILogger Logger { get; set; }

    private static LogHistorySink logHistorySink;

    private static ILogHandler defaultUnityLogHandler;
    private static readonly ILogHandler customUnityLogHandler = new CustomUnityLogHandler();

    private static readonly Subject<LogEvent> logEventStream = new();
    public static IObservable<LogEvent> LogEventStream => logEventStream;

    public static bool IsUsingDefaultUnityLogHandler => defaultUnityLogHandler == null 
                                                        || UnityEngine.Debug.unityLogger.logHandler == defaultUnityLogHandler 
                                                        || Application.isEditor;

    public static LogEventLevel MinimumLogLevel
    {
        get => loggingLevelSwitch.MinimumLevel;
        set => loggingLevelSwitch.MinimumLevel = value;
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        loggingLevelSwitch = new();
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
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
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
            .WriteTo.Sink(new LogEventStreamSink())
            .WriteTo.Sink(new VerboseLogEventForwardToUnitySink());

        ILogger logger = loggerConfiguration.CreateLogger();
        return logger;
    }

    public static string GetLogHistoryAsText(LogEventLevel logEventLevel)
    {
        List<string> logLines = GetLogHistory()
            .Where(logEvent => (int)logEvent.Level >= (int)logEventLevel)
            .Select(logEvent =>
            {
                using StringWriter stringWriter = new();
                textFormatter.Format(logEvent, stringWriter);
                string logLine = stringWriter.ToString();
                return logLine;
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

    public static LogType GetUnityLogType(LogEventLevel logEventLevel)
    {
        switch (logEventLevel)
        {
            case LogEventLevel.Verbose:
            case LogEventLevel.Debug:
            case LogEventLevel.Information:
                return LogType.Log;
            case LogEventLevel.Warning:
                return LogType.Warning;
            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
                return LogType.Error;
            default:
                UnityEngine.Debug.LogError("Unknown LogLevel" + logEventLevel);
                return LogType.Log;
        }
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
                UnityEngine.Debug.LogError("Unknown LogLevel" + logEvent.Level);
                return LogType.Log;
        }
    }
    
    private static void UseCustomUnityLogHandler()
    {
        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = UnityEngine.Debug.unityLogger.logHandler;
        }

        if (UnityEngine.Debug.unityLogger.logHandler == customUnityLogHandler)
        {
            return;
        }

        UnityEngine.Debug.unityLogger.logHandler = customUnityLogHandler;
        UnityEngine.Debug.Log("===== Using Custom Unity Log Handler =====");
    }
    
    private static void UseDefaultUnityLogHandler()
    {
        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = UnityEngine.Debug.unityLogger.logHandler;
        }
        
        if (UnityEngine.Debug.unityLogger.logHandler == defaultUnityLogHandler)
        {
            return;
        }

        UnityEngine.Debug.unityLogger.logHandler = defaultUnityLogHandler;
        UnityEngine.Debug.Log("===== Using Default Unity Log Handler =====");
    }
    
    public static void Verbose(string message)
    {
        DoLog(message, LogEventLevel.Verbose, Logger.Verbose);
    }
    
    public static void Debug(string message)
    {
        DoLog(message, LogEventLevel.Debug, Logger.Debug);
    }

    public static void Information(string message)
    {
        DoLog(message, LogEventLevel.Information, Logger.Information);
    }

    public static void Warning(string message)
    {
        DoLog(message, LogEventLevel.Warning, Logger.Warning);
    }

    public static void Error(string message)
    {
        DoLog(message, LogEventLevel.Error, Logger.Error);
    }

    public static void Exception(Exception ex)
    {
        if (MinimumLogLevel > LogEventLevel.Fatal)
        {
            return;
        }
        
        if (Logger == null
            && MinimumLogLevel <= LogEventLevel.Fatal)
        {
            LogWithDefaultUnityLogHandler(ex);
            return;
        }
        
        Logger.Error(ex, ex.Message);
        if (Application.isEditor)
        {
            LogWithDefaultUnityLogHandler(ex);
        }
    }
    
    private static void DoLog(string message, LogEventLevel logEventLevel, Action<string> doLogWithSerilog)
    {
        if (MinimumLogLevel > logEventLevel)
        {
            return;
        }
        
        if (Logger == null
            && MinimumLogLevel <= logEventLevel)
        {
            LogType logType = GetUnityLogType(logEventLevel);
            LogWithDefaultUnityLogHandler(logType, message);
            return;
        }

        doLogWithSerilog(message);
        if (Application.isEditor)
        {
            LogType logType = GetUnityLogType(logEventLevel);
            LogWithDefaultUnityLogHandler(logType, message);
        }
    }

    private static void LogWithDefaultUnityLogHandler(LogEvent logEvent)
    {
        LogType unityLogType = GetUnityLogType(logEvent);
        using StringWriter stringWriter = new();
        textFormatter.Format(logEvent, stringWriter);
        string logLine = stringWriter.ToString();
        LogWithDefaultUnityLogHandler(unityLogType, logLine);
    }
    
    private static void LogWithDefaultUnityLogHandler(LogType unityLogType, string message)
    {
        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = UnityEngine.Debug.unityLogger.logHandler;
        }
        defaultUnityLogHandler.LogFormat(unityLogType, null, message);
    }
    
    private static void LogWithDefaultUnityLogHandler(Exception ex)
    {
        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = UnityEngine.Debug.unityLogger.logHandler;
        }
        defaultUnityLogHandler.LogException(ex, null);
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
            
            if (Application.isEditor)
            {
                // Forward to UnityEditor's console via defaultUnityLogHandler.
                // This must not be logged again with the Serilog Logger to avoid an infinite loop.
                defaultUnityLogHandler?.LogException(exception, context);
            }
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
    
    private class VerboseLogEventForwardToUnitySink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level
                    is LogEventLevel.Verbose
                    or LogEventLevel.Debug
                && Application.isEditor
                && defaultUnityLogHandler != null)
            {
                LogWithDefaultUnityLogHandler(logEvent);
            }
        }
    }
}
