using System.Collections.Generic;
using System.IO;
using System.Linq;

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
}
