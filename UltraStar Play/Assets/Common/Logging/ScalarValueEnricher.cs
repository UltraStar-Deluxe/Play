using Serilog.Core;
using Serilog.Events;

/**
 * Passes a value to the logging sinks, without the value beeing serialized to a string
 * (i.e. the original object reference is still available in the logging sink).
 */
public class ScalarValueEnricher : ILogEventEnricher
{
    protected readonly LogEventProperty property;

    public ScalarValueEnricher(string name, object value)
    {
        property = new LogEventProperty(name, new ScalarValue(value));
    }

    public void Enrich(LogEvent evt, ILogEventPropertyFactory _)
    {
        evt.AddPropertyIfAbsent(property);
    }
}
