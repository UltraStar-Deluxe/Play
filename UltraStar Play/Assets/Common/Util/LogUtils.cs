using System.Collections.Generic;
using UnityEngine;

public class LogUtils
{
    private static readonly Dictionary<string, LogData> messagePrefixToLogDataMap = new Dictionary<string, LogData>();

    // Logs a message every period.
    // The message is constructed from a messagePrefix and dataPoints that are accumulated over the period.
    // Logging a message every frame quickly gets confusing. This method overcomes this
    // by logging every now and then the combined data from multiple frames.
    public static void LogFreqeuently(string messagePrefix, object dataPoint, float periodInSeconds = 1)
    {
        if (!messagePrefixToLogDataMap.TryGetValue(messagePrefix, out LogData logData))
        {
            logData = new LogData();
            logData.LastLogTimeInSeconds = Time.time;
            messagePrefixToLogDataMap[messagePrefix] = logData;
        }

        logData.DataPointQueue.Add(dataPoint.ToString());

        if (logData.LastLogTimeInSeconds + periodInSeconds < Time.time)
        {
            string dataPointsCsv = string.Join(", ", logData.DataPointQueue);
            Debug.Log($"{messagePrefix} [{dataPointsCsv}]");
            logData.DataPointQueue.Clear();
            logData.LastLogTimeInSeconds = Time.time;
        }
    }

    private class LogData
    {
        public List<string> DataPointQueue { get; private set; } = new List<string>();
        public float LastLogTimeInSeconds { get; set; }
    }
}