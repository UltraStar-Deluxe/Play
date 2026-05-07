using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DirectoryScanner
{
    public static List<string> GetFolders(List<string> folders, DirectoryScannerConfig config)
    {
        return folders
            .SelectMany(folder => GetFolders(folder, config))
            .ToList();
    }

    public static List<string> GetFolders(string folder, DirectoryScannerConfig config)
    {
        if (folder.IsNullOrEmpty()
            || !Directory.Exists(folder))
        {
            return new List<string>();
        }

        List<string> unfilteredResult = new();

        SearchOption searchOption = config.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        foreach (string searchPattern in config.SearchPatterns)
        {
            string[] filesOfPattern = Directory.GetDirectories(folder, searchPattern, searchOption);
            unfilteredResult.AddRange(filesOfPattern);
        }

        if (!config.ExcludeHiddenFolders)
        {
            return unfilteredResult;
        }

        List<string> filteredResult = unfilteredResult
            // Ignore hidden folders
            .Where(folderPath => (!config.ExcludeHiddenFolders || !IsHiddenFolder(folderPath, folder)))
            .ToList();

        return filteredResult;
    }

    private static bool IsHiddenFolder(string folderPath, string rootPath)
    {
        // On Unix based operating systems, files and folders that start with a dot are hidden.
        // Only check for hidden folders that are children of the root path, not the root path
        // itself or its parents.
        // We know rootPath is always a prefix of filePath, so it can be cut out with Substring().
        folderPath = folderPath.Substring(rootPath.Length);
        return folderPath.StartsWith(".") || folderPath.Contains("\\.") || folderPath.Contains("/.");
    }
}
