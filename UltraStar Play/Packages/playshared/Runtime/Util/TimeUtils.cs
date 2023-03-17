using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class TimeUtils
{
    /**
     * Returns the current system time in milliseconds like it is done on Unix systems.
     * Unity's Time.time can only be called from the main thread.
     * This class provides alternatives.
     */
    public static long GetUnixTimeMilliseconds()
    {
        // See https://stackoverflow.com/questions/4016483/get-time-in-milliseconds-using-c-sharp
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public static bool IsDurationAboveThreshold(float unityStartTimeInSeconds, float thresholdInSeconds)
    {
        float durationInSeconds = Time.time - unityStartTimeInSeconds;
        return durationInSeconds > thresholdInSeconds;
    }
    
    public static bool IsDurationAboveThreshold(long unixStartTimeInMillis, long thresholdInMillis)
    {
        float durationInMillis = GetUnixTimeMilliseconds() - unixStartTimeInMillis;
        return durationInMillis > thresholdInMillis;
    }

    /**
     * Parses a duration like "0.5 s" and "1000ms" to a TimeSpan.
     */
    public static bool TryParseDuration(string text, out long durationInMilliseconds, string defaultTimeUnit = "")
    {
        HashSet<string> supportedTimeUnits = new() { "ms", "s" };
        if (!defaultTimeUnit.IsNullOrEmpty()
            && !supportedTimeUnits.Contains(defaultTimeUnit))
        {
            throw new ArgumentException($"Unhandled default time unit {defaultTimeUnit}");
        }

        bool IsMatchingTimeUnit(string actualTimeUnit, string expectedTimeUnit)
        {
            return actualTimeUnit == expectedTimeUnit
                   || (actualTimeUnit.IsNullOrEmpty()
                       && !defaultTimeUnit.IsNullOrEmpty()
                       && defaultTimeUnit == expectedTimeUnit);
        }

        try
        {
            text = text.Trim()
                .Replace(" ", "")
                // Unify decimal separator
                .Replace(",", ".");
            string timeUnit = GetTimeUnit(text);

            text = text.Trim();
            if (IsMatchingTimeUnit(timeUnit, "ms"))
            {
                string numberPart = text.Replace("ms", "");
                durationInMilliseconds = (long)double.Parse(numberPart, CultureInfo.InvariantCulture);
                return true;
            }

            if (IsMatchingTimeUnit(timeUnit, "s"))
            {
                string numberPart = text.Replace("s", "");
                double seconds = double.Parse(numberPart, CultureInfo.InvariantCulture);
                durationInMilliseconds = (long)Math.Round(seconds * 1000);
                return true;
            }

            throw new ArgumentException($"Unhandled time unit '{timeUnit}'");
        }
        catch (Exception ex)
        {
            Debug.LogError(new ArgumentException($"Could not parse duration of {text}", ex));
        }
        durationInMilliseconds = 0;
        return false;
    }

    private static string GetTimeUnit(string text)
    {
        if (text.EndsWith("ms"))
        {
            return "ms";
        }

        if (text.EndsWith("s"))
        {
            return "s";
        }

        return "";
    }
}
