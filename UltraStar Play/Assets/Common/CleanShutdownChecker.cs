using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/**
 * Tracks application start and successful shutdown timestamps in files under Application.persistentDataPath
 * to detect whether the previous session ended cleanly.
 */
public class CleanShutdownChecker : MonoBehaviour
{
    private string LastSuccessfulStartFilePath => ApplicationUtils.GetPersistentDataPath("LastSuccessfulStart.txt");
    private string LastSuccessfulShutdownFilePath => ApplicationUtils.GetPersistentDataPath("LastSuccessfulShutdown.txt");
    
    public bool WasLastShutdownClean { get; private set; }

    private void Awake()
    {
        WasLastShutdownClean = ShutdownTimestampAfterStartTimestamp();
        if (!WasLastShutdownClean)
        {
            Debug.LogWarning("Found mismatch of last successful start and last successful shutdown files. Assuming that previous session was terminated unexpectedly.");
        }
        
        WriteTimestampFile(LastSuccessfulStartFilePath, DateTime.Now);
    }

    private void OnApplicationQuit()
    {
        WriteTimestampFile(LastSuccessfulShutdownFilePath, DateTime.Now);
    }

    private bool ShutdownTimestampAfterStartTimestamp()
    {
        if (!TryReadTimestamp(LastSuccessfulStartFilePath, out DateTime lastStart))
        {
            // No info about last start => assume initial start and thus a clean shutdown.
            return true;
        }
        
        if (!TryReadTimestamp(LastSuccessfulShutdownFilePath, out DateTime lastShutdown))
        {
            // Failed to read last shutdown time => assume unclean shutdown.
            return false;
        }
        return lastShutdown >= lastStart;
    }

    private static bool TryReadTimestamp(string filePath, out DateTime timestamp)
    {
        try
        {
            if (!FileUtils.Exists(filePath))
            {
                timestamp = default;
                return false;
            }
            
            string text = File.ReadAllText(filePath).Trim();
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to read timestamp file. Path: '{filePath}'");
        }
        timestamp = default;
        return false;
    }

    private static void WriteTimestampFile(string filePath, DateTime dateTime)
    {
        try
        {
            File.WriteAllText(filePath, dateTime.ToString("o", CultureInfo.InvariantCulture));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to write timestamp file. Path: '{filePath}'");
        }
    }
}
