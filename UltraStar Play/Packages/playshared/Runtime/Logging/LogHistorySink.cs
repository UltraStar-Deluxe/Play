using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

public class LogHistorySink : ILogEventSink
{
    private const int MaxLogHistorySize = 10000;
    public List<LogEvent> LogHistory { get; private set; }= new();

    public void Emit(LogEvent logEvent)
    {
        if (LogHistory.Count > MaxLogHistorySize)
        {
            LogHistory.RemoveAt(0);
        }
        LogHistory.Add(logEvent);
    }
}
