using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class FileScanner
{
    public static List<string> GetFiles(List<string> folders, FileScannerConfig config)
    {
        return folders
            .SelectMany(folder => GetFiles(folder, config))
            .ToList();
    }

    public static List<string> GetFiles(string folder, FileScannerConfig config)
    {
        if (config.SearchPatterns.IsNullOrEmpty())
        {
            throw new ArgumentException(nameof(config.SearchPatterns));
        }

        if (folder.IsNullOrEmpty()
            || !Directory.Exists(folder))
        {
            return new List<string>();
        }

        List<string> unfilteredResult = new();

        SearchOption searchOption = config.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        foreach (string fileExtensionPattern in config.SearchPatterns)
        {
            try
            {
                string[] filesOfPattern = Directory.GetFiles(folder, fileExtensionPattern, searchOption);
                unfilteredResult.AddRange(filesOfPattern);
            }
            catch (Exception e)
            {
                e.Log($"Failed to get files of pattern. Folder='{folder}', Pattern='{fileExtensionPattern}', SearchOption: '{searchOption}'");
            }
        }

        if (!config.ExcludeHiddenFolders
            && !config.ExcludeHiddenFiles)
        {
            return unfilteredResult;
        }

        List<string> filteredResult = unfilteredResult
            // Ignore hidden files and folders
            .Where(filePath => (!config.ExcludeHiddenFiles || !IsHiddenFile(filePath))
                               && (!config.ExcludeHiddenFolders || !IsInsideHiddenFolder(filePath)))
            .ToList();

        return filteredResult;
    }

    private static bool IsHiddenFile(string filePath)
    {
        // On Unix based operating systems, files and folders that start with a dot are hidden.
        return Path.GetFileName(filePath).StartsWith(".");
    }

    private static bool IsInsideHiddenFolder(string filePath)
    {
        // On Unix based operating systems, files and folders that start with a dot are hidden.
        string folderPath = Path.GetDirectoryName(filePath);
        return folderPath.Contains("\\.") || folderPath.Contains("/.");
    }
}
