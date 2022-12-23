using System.IO;

public static class DirectoryUtils
{
    public static bool IsSubDirectory(string potentialSubDirectory, string potentialAncestorDirectory)
    {
        string potentialAncestorDirectoryFullName = new DirectoryInfo(potentialAncestorDirectory).FullName;
        string potentialSubDirectoryFullName = new DirectoryInfo(potentialSubDirectory).FullName;
        return potentialSubDirectoryFullName.StartsWith(potentialAncestorDirectoryFullName);
    }
}
