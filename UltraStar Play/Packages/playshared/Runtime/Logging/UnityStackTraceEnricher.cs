using Serilog.Core;
using Serilog.Events;
using UnityEngine;

public class UnityStackTraceEnricher : ILogEventEnricher
{
    private static readonly string indentation = "    ";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception != null)
        {
            string stackTraceIndented = indentation + StackTraceUtility.ExtractStringFromException(logEvent.Exception)
                .Replace("\r\n", "\n")
                .Replace("\n", "\n" + indentation);
            logEvent.AddOrUpdateProperty(
                new LogEventProperty("StackTrace", new ScalarValue(stackTraceIndented)));
        }
    }
}
