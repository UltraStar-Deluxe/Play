using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Events;
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
    
    public static List<string> GetFilesInFolder(string folderPath, params string[] fileExtensions)
    {
        List<string> result = new();
        foreach (string fileExtension in fileExtensions)
        {
            string[] files = Directory.GetFiles(folderPath, fileExtension, SearchOption.AllDirectories);
            result.AddRange(files);
        }

        return result
            .Distinct()
            .ToList();
    }
    
    public static bool IsSubDirectory(string potentialSubDirectory, string potentialAncestorDirectory)
    {
        string potentialAncestorDirectoryFullName = new DirectoryInfo(potentialAncestorDirectory).FullName;
        string potentialSubDirectoryFullName = new DirectoryInfo(potentialSubDirectory).FullName;
        return potentialSubDirectoryFullName.StartsWith(potentialAncestorDirectoryFullName);
    }

    public static bool Exists(string directory)
    {
        return !directory.IsNullOrEmpty() && Directory.Exists(directory);
    }

    public static void Delete(string path)
    {
        if (path.IsNullOrEmpty()
            || !Exists(path))
        {
            return;
        }
        
        Directory.Delete(path, true);
    }
    
    public static void CopyAll(string sourceDirectory, string targetDirectory, LogEventLevel logEventLevel = LogEventLevel.Verbose)
    {
        if (sourceDirectory.IsNullOrEmpty()
            || targetDirectory.IsNullOrEmpty())
        {
            return;
        }
    
        Debug.Log($"Copying folder '{sourceDirectory}' to '{targetDirectory}'");
         
        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget, logEventLevel);
    }

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target, LogEventLevel logEventLevel = LogEventLevel.Verbose)
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
            Log.WithLevel(logEventLevel, () => $"Copying '{sourceFileName}' to '{targetFileName}'");
            fi.CopyTo(targetFileName, true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}
