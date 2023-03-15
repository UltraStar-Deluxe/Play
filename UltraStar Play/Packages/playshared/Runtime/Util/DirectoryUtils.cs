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
}
