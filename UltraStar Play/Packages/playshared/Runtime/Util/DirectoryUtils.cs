using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DirectoryUtils
{
    public static void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static bool IsSubDirectory(string potentialSubDirectory, string potentialAncestorDirectory)
    {
        string potentialSubDirectoryNormalized = potentialSubDirectory.Replace("\\", "/");
        string potentialAncestorDirectoryNormalized = potentialSubDirectory.Replace("\\", "/");
        if (potentialSubDirectoryNormalized.StartsWith(potentialAncestorDirectoryNormalized.Normalize()))
        {
            return true;
        }

        if (!Exists(potentialSubDirectory)
            || !Exists(potentialAncestorDirectory))
        {
            return false;
        }

        string potentialAncestorDirectoryFullName = new DirectoryInfo(potentialAncestorDirectory).FullName;
        string potentialSubDirectoryFullName = new DirectoryInfo(potentialSubDirectory).FullName;
        return potentialSubDirectoryFullName.StartsWith(potentialAncestorDirectoryFullName);
    }

    public static bool Exists(string directory)
    {
        return !directory.IsNullOrEmpty() && Directory.Exists(directory);
    }

    public static void Delete(string path, bool recusive)
    {
        if (path.IsNullOrEmpty()
            || !Exists(path))
        {
            return;
        }

        Directory.Delete(path, recusive);
    }

    public static void CopyAll(
        string sourceDirectory,
        string targetDirectory,
        CopyDirectoryFilter filter = null,
        ELogEventLevel logEventLevel = ELogEventLevel.Verbose)
    {
        if (sourceDirectory.IsNullOrEmpty()
            || targetDirectory.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"Copying folder '{sourceDirectory}' to '{targetDirectory}'");

        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget, filter, logEventLevel);
    }

    public static void CopyAll(
        DirectoryInfo source,
        DirectoryInfo target,
        CopyDirectoryFilter filter = null,
        ELogEventLevel logEventLevel = ELogEventLevel.Verbose)
    {
        // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        if (source == null
            || target == null)
        {
            return;
        }

        Log.WithLevel(logEventLevel, () => $"Copying folder '{source}' to '{target}'");

        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            string sourceFileName = fi.FullName;
            string targetFileName = Path.Combine(target.FullName, fi.Name);

            if (filter != null
                && filter.IsExcluded != null
                && filter.IsExcluded(sourceFileName))
            {
                Log.WithLevel(logEventLevel, () => $"Ignoring '{sourceFileName}'");
                continue;
            }

            if (filter == null
                || filter.IsIncluded == null
                || filter.IsIncluded(sourceFileName))
            {
                Log.WithLevel(logEventLevel, () => $"Copying '{sourceFileName}' to '{targetFileName}'");
                fi.CopyTo(targetFileName, true);
            }
            else
            {
                Log.WithLevel(logEventLevel, () => $"Ignoring '{sourceFileName}'");
            }
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo sourceSubDir in source.GetDirectories())
        {
            string sourceSubDirectoryPath = sourceSubDir.FullName;
            if (filter != null
                && filter.IsExcluded != null
                && filter.IsExcluded(sourceSubDirectoryPath))
            {
                Log.WithLevel(logEventLevel, () => $"Ignoring '{sourceSubDirectoryPath}'");
                continue;
            }

            if (filter == null
                || filter.IsIncluded == null
                || filter.IsIncluded(sourceSubDirectoryPath))
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
                CopyAll(sourceSubDir, nextTargetSubDir, filter, logEventLevel);
            }
            else
            {
                Log.WithLevel(logEventLevel, () => $"Ignoring '{sourceSubDirectoryPath}'");
            }
        }
    }

    public static List<DirectoryInfo> GetParentDirectories(DirectoryInfo directory, bool includeInitialDirectory = false)
    {
        List<DirectoryInfo> result = new List<DirectoryInfo>();
        if (includeInitialDirectory)
        {
            result.Add(directory);
        }

        while (directory != null
               && directory.Parent != null)
        {
            result.Add(directory.Parent);
            directory = directory.Parent;
        }
        return result;
    }
}
