using System;
using System.Diagnostics;

// A stopwatch that performs an action when it is disposed.
public class DisposableStopwatch : IDisposable
{
    private Stopwatch stopwatch;
    private readonly Action<Stopwatch> action;

    public DisposableStopwatch(Action<Stopwatch> action, bool startStopwatch = true)
    {
        this.action = action;
        CreateStopwatch(startStopwatch);
    }

    // Logs the given text when the stopwatch is disposed.
    // Thereby, the placeholder <millis> will be replaced with the elapsed milliseconds.
    public DisposableStopwatch(string textWithPlaceholders, bool startStopwatch = true)
    {
        action = (sw) => LogTextWithPlaceholders(sw, textWithPlaceholders);
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

    private void LogTextWithPlaceholders(Stopwatch sw, string textWithPlaceholders)
    {
        string millis = sw.ElapsedMilliseconds.ToString();
        string logText = textWithPlaceholders.Replace("<millis>", millis);
        UnityEngine.Debug.Log(logText);
    }
}