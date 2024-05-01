using System;
using Serilog.Events;

public static class ELogEventLevelExtensions
{
    public static LogEventLevel ToSerilogLogEventLevel(this ELogEventLevel logEventLevel)
    {
        switch (logEventLevel)
        {
            case ELogEventLevel.Verbose: return LogEventLevel.Verbose;
            case ELogEventLevel.Debug: return LogEventLevel.Debug;
            case ELogEventLevel.Information: return LogEventLevel.Information;
            case ELogEventLevel.Warning: return LogEventLevel.Warning;
            case ELogEventLevel.Error: return LogEventLevel.Error;
            case ELogEventLevel.Fatal: return LogEventLevel.Fatal;
            default:
                throw new ArgumentOutOfRangeException(nameof(logEventLevel), logEventLevel, null);
        }
    }

    public static ELogEventLevel ToCustomLogEventLevel(this LogEventLevel logEventLevel)
    {
        switch (logEventLevel)
        {
            case LogEventLevel.Verbose: return ELogEventLevel.Verbose;
            case LogEventLevel.Debug: return ELogEventLevel.Debug;
            case LogEventLevel.Information: return ELogEventLevel.Information;
            case LogEventLevel.Warning: return ELogEventLevel.Warning;
            case LogEventLevel.Error: return ELogEventLevel.Error;
            case LogEventLevel.Fatal: return ELogEventLevel.Fatal;
            default:
                throw new ArgumentOutOfRangeException(nameof(logEventLevel), logEventLevel, null);
        }
    }
}
