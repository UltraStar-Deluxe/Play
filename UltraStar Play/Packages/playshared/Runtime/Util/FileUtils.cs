using System.IO;
using System.Text;
using UnityEngine;

public static class FileUtils
{
    public static void WriteAllTextIfChanged(string targetPath, string text)
    {
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
            File.WriteAllText(targetPath, text, Encoding.UTF8);
            Debug.Log("Updated file " + targetPath);
        }
        else
        {
            Debug.Log("File still up-to-date " + targetPath);
        }
    }
}
