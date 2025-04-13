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

    public static ELogEventLevel MinimumLogLevel
    {
        get => MinimumSerilogLogLevel.ToCustomLogEventLevel();
        set => MinimumSerilogLogLevel = value.ToSerilogLogEventLevel();
    }

    private static LogEventLevel MinimumSerilogLogLevel
    {
        get
        {
            if (loggingLevelSwitch == null)
            {
                return LogEventLevel.Information;
            }
            return loggingLevelSwitch.MinimumLevel;
        }
        set
        {
            if (loggingLevelSwitch == null)
            {
                return;
            }
            loggingLevelSwitch.MinimumLevel = value;
        }
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
            .WriteTo.Sink(new LogEventStreamSink());

        ILogger logger = loggerConfiguration.CreateLogger();
        return logger;
    }

    public static string GetLogHistoryAsText(ELogEventLevel logEventLevel)
    {
        List<string> logLines = GetLogHistory()
            .Where(logEvent => (int)logEvent.Level >= (int)logEventLevel.ToSerilogLogEventLevel())
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

    private static string GetSerilogLogMessage(Object context, string format, params object[] args)
    {
        if (context == null)
        {
            return string.Format(format, args);
        }

        return string.Format($"[{context.name}] {format}", args);
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

    public static void Verbose(Func<string> messageGetter)
    {
        if (Logger != null)
        {
            DoLog(messageGetter, LogEventLevel.Verbose, Logger.Verbose);
        }
        else
        {
            DoLog(messageGetter, LogEventLevel.Verbose, null);
        }
    }

    public static void Debug(Func<string> messageGetter)
    {
        if (Logger != null)
        {
            DoLog(messageGetter, LogEventLevel.Debug, Logger.Debug);
        }
        else
        {
            DoLog(messageGetter, LogEventLevel.Debug, null);
        }
    }

    public static void Information(Func<string> messageGetter)
    {
        if (Logger != null)
        {
            DoLog(messageGetter, LogEventLevel.Information, Logger.Information);
        }
        else
        {
            DoLog(messageGetter, LogEventLevel.Information, null);
        }
    }

    public static void Warning(Func<string> messageGetter)
    {
        if (Logger != null)
        {
            DoLog(messageGetter, LogEventLevel.Warning, Logger.Warning);
        }
        else
        {
            DoLog(messageGetter, LogEventLevel.Warning, null);
        }
    }

    public static void Error(Func<string> messageGetter)
    {
        if (Logger != null)
        {
            DoLog(messageGetter, LogEventLevel.Error, Logger.Error);
        }
        else
        {
            DoLog(messageGetter, LogEventLevel.Error, null);
        }
    }

    public static void Exception(Func<Exception> exceptionGetter)
    {
        if (MinimumSerilogLogLevel > LogEventLevel.Fatal)
        {
            return;
        }

        Exception ex = exceptionGetter();

        if (Logger == null
            && MinimumSerilogLogLevel <= LogEventLevel.Fatal)
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

    private static void DoLog(Func<string> messageGetter, LogEventLevel logEventLevel, Action<string> doLogWithSerilog)
    {
        if (MinimumSerilogLogLevel > logEventLevel)
        {
            return;
        }

        string message = messageGetter();

        if (Logger == null
            && MinimumSerilogLogLevel <= logEventLevel)
        {
            LogType logType = GetUnityLogType(logEventLevel);
            LogWithDefaultUnityLogHandler(logType, message);
            return;
        }

        doLogWithSerilog?.Invoke(message);
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
        if (!Application.isEditor)
        {
            return;
        }

        if (defaultUnityLogHandler == null)
        {
            defaultUnityLogHandler = UnityEngine.Debug.unityLogger.logHandler;
        }

        try
        {
            defaultUnityLogHandler.LogFormat(unityLogType, null, message);
        }
        catch (FormatException formatException1)
        {
            try
            {
                string messageWithEscapedCurlyBraces = message.Replace("{", "{{").Replace("}", "}}");
                defaultUnityLogHandler.LogFormat(unityLogType, null, messageWithEscapedCurlyBraces);
            }
            catch (FormatException formatException2)
            {
                string messageWithEscapedCurlyBraces = message.Replace("{", "CURLY_OPEN").Replace("}", "CURLY_CLOSE");
                defaultUnityLogHandler.LogFormat(unityLogType, null, messageWithEscapedCurlyBraces);
            }
        }
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
                    Logger.Information(GetSerilogLogMessage(context, format, args));
                    break;
                case LogType.Warning:
                    Logger.Warning(GetSerilogLogMessage(context, format, args));
                    break;
                case LogType.Error:
                    Logger.Error(GetSerilogLogMessage(context, format, args));
                    break;
                case LogType.Exception:
                    Logger.Error(GetSerilogLogMessage(context, format, args));
                    break;
                case LogType.Assert:
                    Logger.Fatal(GetSerilogLogMessage(context, format, args));
                    break;
                default:
                    Logger.Information(GetSerilogLogMessage(context, format, args));
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

            Logger.Error(exception, GetSerilogLogMessage(context, "{0}", exception.Message));

            if (Application.isEditor)
            {
                // Forward to UnityEditor's console via defaultUnityLogHandler.
                // This must not be logged again with the Serilog Logger to avoid an infinite loop.
                defaultUnityLogHandler?.LogException(exception, context);
            }
        }
    }

    private class LogEventStreamSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            logEventStream.OnNext(logEvent);
        }
    }

    public static void WithLevel(ELogEventLevel logLevel, Func<string> messageGetter)
    {
        switch (logLevel)
        {
            case ELogEventLevel.Verbose:
                Verbose(messageGetter);
                break;
            case ELogEventLevel.Debug:
                Debug(messageGetter);
                break;
            case ELogEventLevel.Information:
                Information(messageGetter);
                break;
            case ELogEventLevel.Warning:
                Warning(messageGetter);
                break;
            case ELogEventLevel.Error:
                Error(messageGetter);
                break;
            case ELogEventLevel.Fatal:
                Error(messageGetter);
                break;
            default:
                Information(messageGetter);
                break;
        }
    }
}
