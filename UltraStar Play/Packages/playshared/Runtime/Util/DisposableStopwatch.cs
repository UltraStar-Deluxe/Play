using System;
using System.Diagnostics;

// A stopwatch that performs an action when it is disposed.
public class DisposableStopwatch : IDisposable
{
    private Stopwatch stopwatch;
    private readonly Action<Stopwatch> action;

    public double ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;
    public TimeSpan Elapsed => stopwatch.Elapsed;
    public long ElapsedTicks => stopwatch.ElapsedTicks;

    public DisposableStopwatch(Action<Stopwatch> action, bool startStopwatch = true)
    {
        this.action = action;
        CreateStopwatch(startStopwatch);
    }

    // Logs the given text when the stopwatch is disposed.
    // Thereby, the placeholder <millis> will be replaced with the elapsed milliseconds.
    public DisposableStopwatch(string textWithPlaceholders, bool startStopwatch = true, float logPeriodInSeconds = 0)
    {
        action = (sw) => LogTextWithPlaceholders(sw, textWithPlaceholders, logPeriodInSeconds);
        CreateStopwatch(startStopwatch);
    }

    public void Dispose()
    {
        stopwatch.Stop();
        action.Invoke(stopwatch);
    }

    private void CreateStopwatch(bool startStopwatch)
    {
        stopwatch = new Stopwatch();
        if (startStopwatch)
        {
            stopwatch.Start();
        }
    }

    private void LogTextWithPlaceholders(Stopwatch sw, string textWithPlaceholders, float logPeriodInSeconds)
    {
        string millis = sw.ElapsedMilliseconds.ToString();
        string logText = textWithPlaceholders
            .Replace("<millis>", millis)
            .Replace("<ms>", millis);
        if (logPeriodInSeconds <= 0)
        {
            // Log immediately
            UnityEngine.Debug.Log(logText);
        }
        else
        {
            // Log every now and then the measured durations
            LogUtils.LogFrequently(textWithPlaceholders, millis, logPeriodInSeconds);
        }
    }
}
