using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class FileScanner
{
    private readonly List<string> fileExtensionPatterns;
    private readonly bool excludeHiddenFolders;
    private readonly bool excludeHiddenFiles;

    public FileScanner(string fileExtensionPattern, bool excludeHiddenFolders, bool excludeHiddenFiles)
        : this(new List<string> { fileExtensionPattern }, excludeHiddenFolders, excludeHiddenFiles)
    {
    }

    public FileScanner(List<string> fileExtensionPatterns, bool excludeHiddenFolders, bool excludeHiddenFiles)
    {
        this.fileExtensionPatterns = fileExtensionPatterns;
        this.excludeHiddenFolders = excludeHiddenFolders;
        this.excludeHiddenFiles = excludeHiddenFiles;

        // Checks
        if (this.fileExtensionPatterns.IsNullOrEmpty())
        {
            throw new UnityException("Can not scan for files. No file extensions specified.");
        }
    }

    public List<string> GetFiles(string folder, bool recursive)
    {
        if (folder.IsNullOrEmpty()
            || !Directory.Exists(folder))
        {
            return new List<string>();
        }

        List<string> unfilteredResult = new();

        SearchOption searchOption = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        foreach (string fileExtensionPattern in fileExtensionPatterns)
        {
            string[] filesOfPattern = Directory.GetFiles(folder, fileExtensionPattern, searchOption);
            unfilteredResult.AddRange(filesOfPattern);
        }

        if (!excludeHiddenFolders
            && !excludeHiddenFiles)
        {
            return unfilteredResult;
        }

        List<string> filteredResult = unfilteredResult
            // Ignore hidden files and folders
            .Where(filePath => (!excludeHiddenFiles || !IsHiddenFile(filePath))
                               && (!excludeHiddenFolders || !IsInsideHiddenFolder(filePath)))
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
