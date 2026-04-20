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
                               && (!config.ExcludeHiddenFolders || !IsInsideHiddenFolder(filePath, folder)))
            .ToList();

        return filteredResult;
    }

    private static bool IsHiddenFile(string filePath)
    {
        // On Unix based operating systems, files and folders that start with a dot are hidden.
        return Path.GetFileName(filePath).StartsWith(".");
    }

    private static bool IsInsideHiddenFolder(string filePath, string rootPath)
    {
        // On Unix based operating systems, files and folders that start with a dot are hidden.
        // Only check for hidden folders that are children of the root path, not the root path
        // itself or its parents.
        // We know rootPath is always a prefix of filePath, so it can be cut out with Substring().
        // We also need to cut out the filename. Path.GetDirectoryName() can not be used here
        // because it removes duplicate and trailing slashes, changing the string length.
        string folderPath = filePath.Substring(rootPath.Length, filePath.LastIndexOfAny(new[] { '/', '\\' }) + 1 - rootPath.Length);
        return folderPath.StartsWith(".") || folderPath.Contains("\\.") || folderPath.Contains("/.");
    }
}
