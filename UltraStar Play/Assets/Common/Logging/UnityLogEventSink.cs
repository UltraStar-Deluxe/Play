using System.IO;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using UnityEngine;

public sealed class UnityLogEventSink : ILogEventSink
{
    private static readonly string defaultOutputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{StackTrace}";

    public static readonly string unityLogEventSinkMarker = "\n(UnityLogEventSink)";
    public static readonly string skipUnityLogEventSinkPropertyName = "skipUnityLogEventSink";

    private readonly ITextFormatter formatter;

    public UnityLogEventSink()
        : this(new MessageTemplateTextFormatter(defaultOutputTemplate, null))
    {
    }

    public UnityLogEventSink(ITextFormatter formatter)
    {
        this.formatter = formatter;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Properties.ContainsKey(skipUnityLogEventSinkPropertyName))
        {
            return;
        }

        using (StringWriter stringBuffer = new StringWriter())
        {
            formatter.Format(logEvent, stringBuffer);
            // Need to escape curly braces because Debug.LogFormat is used
            string logString = stringBuffer.ToString().Trim()
                .Replace("{","{{")
                .Replace("}", "}}")
                + unityLogEventSinkMarker;
            LogType logType = GetUnityLogType(logEvent);
            UnityEngine.Object contextObject = GetUnityEngineContextObject(logEvent);
            Debug.LogFormat(logType, LogOption.NoStacktrace, contextObject, logString);
        }
    }

    private UnityEngine.Object GetUnityEngineContextObject(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue(UnityObjectEnricher.unityObjectPropertyName, out LogEventPropertyValue logEventPropertyValue)
            && logEventPropertyValue is ScalarValue scalarValue)
        {
            return scalarValue.Value as UnityEngine.Object;
        }
        return null;
    }

    private static LogType GetUnityLogType(LogEvent logEvent)
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
                Debug.LogError("Unkown LogLevel" + logEvent.Level);
                return LogType.Log;
        }
    }
}
