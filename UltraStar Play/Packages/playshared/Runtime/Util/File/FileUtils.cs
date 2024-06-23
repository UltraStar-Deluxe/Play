using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

public static class FileUtils
{
    public static string ReadAllText(string targetPath)
    {
        if (!Exists(targetPath))
        {
            return "";
        }

        return File.ReadAllText(targetPath);
    }

    public static void WriteAllText(string targetPath, string text, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        File.WriteAllText(targetPath, text, encoding);
    }

    public static void WriteAllTextIfChanged(string targetPath, string text, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;

        string NormalizeText(string t)
        {
            // Normalize line endings.
            return t.Replace("\r", "");
        }

        // Only write file if the code changed. Otherwise it can lead to an endless loop of recompiling.
        string oldText = File.Exists(targetPath)
            ? File.ReadAllText(targetPath, Encoding.UTF8)
            : "";

        if (NormalizeText(oldText) != NormalizeText(text))
        {
            File.WriteAllText(targetPath, text, encoding);
            Debug.Log("Updated file " + targetPath);
        }
        else
        {
            Debug.Log("File still up-to-date " + targetPath);
        }
    }

    public static void MoveFileOverwriteIfExists(string sourceFile, string destinationFile)
    {
        if (File.Exists(destinationFile))
        {
            File.Delete(destinationFile);
        }

        if (File.Exists(destinationFile))
        {
            // Wait a moment such that the OS can delete the file
            Thread.Sleep(100);
        }
        File.Move(sourceFile, destinationFile);
    }

    public static bool Exists(string path)
    {
        return !path.IsNullOrEmpty() && File.Exists(path);
    }

    public static void Copy(string sourcePath, string targetPath, bool overwrite)
    {
        if (sourcePath.IsNullOrEmpty()
            || targetPath.IsNullOrEmpty()
            || (Exists(targetPath) && !overwrite)
            || !File.Exists(sourcePath))
        {
            return;
        }

        string targetFolder = Path.GetDirectoryName(targetPath);
        if (!targetFolder.IsNullOrEmpty()
            && !Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        File.Copy(sourcePath, targetPath, overwrite);
    }

    public static void SleepUntilFileExists(string newPlaylistPath, int maxWaitTimeInMillis)
    {
        if (File.Exists(newPlaylistPath))
        {
            return;
        }

        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        while (!File.Exists(newPlaylistPath)
               && !TimeUtils.IsDurationAboveThresholdInMillis(startTime, maxWaitTimeInMillis))
        {
            Thread.Sleep(10);
        }
    }

    public static void Delete(string dbPath, long maxWaitTineInMillis = 100)
    {
        if (!File.Exists(dbPath))
        {
            return;
        }

        File.Delete(dbPath);
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        while (File.Exists(dbPath)
               && !TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTineInMillis))
        {
            Thread.Sleep(1);
        }

        if (File.Exists(dbPath))
        {
            throw new IOException($"Failed to delete file {dbPath} within {maxWaitTineInMillis}");
        }
    }
}
