using System;

/**
 * Convience method to add context information to log messages.
 */
// TODO: Customize Logger instance with ForContext instead?
public class LogWithContext
{
    private readonly string context;

    public LogWithContext(string context)
    {
        this.context = context;
    }

    private string FormatMessage(string message)
    {
        return $"[{context}] {message}";
    }

    public void Verbose(Func<string> messageGetter)
    {
        Log.Verbose(() => FormatMessage(messageGetter()));
    }

    public void Debug(Func<string> messageGetter)
    {
        Log.Debug(() => FormatMessage(messageGetter()));
    }

    public void Information(Func<string> messageGetter)
    {
        Log.Information(() => FormatMessage(messageGetter()));
    }

    public void Warning(Func<string> messageGetter)
    {
        Log.Warning(() => FormatMessage(messageGetter()));
    }

    public void Error(Func<string> messageGetter)
    {
        Log.Error(() => FormatMessage(messageGetter()));
    }

    public void Exception(Func<Exception> exceptionGetter)
    {
        Log.Exception(exceptionGetter);
    }
}
