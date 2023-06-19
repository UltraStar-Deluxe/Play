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
                .Replace("\n\n", "\n")
                .Replace("\r\n", "\n")
                .Replace("\n", "\n" + indentation);
            // Add additional newline to separate stack trace from next message (making it easier to find in the log file.
            stackTraceIndented += "\n";
            logEvent.AddOrUpdateProperty(
                new LogEventProperty("StackTrace", new ScalarValue(stackTraceIndented)));
        }
    }
}
