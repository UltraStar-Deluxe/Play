using System;
using System.Collections.Generic;
using UnityEngine;

public static class FileScannerUtils
{
    public static List<string> ScanForFiles(List<string> folders, List<string> fileExtensionPatterns)
    {
        FileScanner fileScanner = new(fileExtensionPatterns, true, true);
        List<string> files = new();
        foreach (string folder in folders)
        {
            try
            {
                List<string> txtFilesInSongDir = fileScanner.GetFiles(folder, true);
                files.AddRange(txtFilesInSongDir);

                Log.Debug(() => $"Found {txtFilesInSongDir.Count} files matching patterns {fileExtensionPatterns.JoinWith(", ")} in folder: '{folder}'");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        return files;
    }
}
