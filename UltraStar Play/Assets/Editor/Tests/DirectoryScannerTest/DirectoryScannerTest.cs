using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DirectoryScannerTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/DirectoryScannerTest/";

    private static readonly string searchPattern = "*A";

    [Test]
    public void NonRecursive()
    {
        List<string> folders = DirectoryScanner.GetFolders(folderPath,
            new DirectoryScannerConfig() { ExcludeHiddenFolders = false, Recursive = false });
        Assert.AreEqual(2, folders.Count);
    }

    [Test]
    public void NonRecursiveSearchPattern()
    {
        List<string> folders = DirectoryScanner.GetFolders(folderPath,
            new DirectoryScannerConfig(searchPattern) { ExcludeHiddenFolders = false, Recursive = false });
        Assert.AreEqual(1, folders.Count);
    }

    [Test]
    public void Recursive()
    {
        List<string> folders = DirectoryScanner.GetFolders(folderPath,
            new DirectoryScannerConfig() { ExcludeHiddenFolders = false, Recursive = true });
        Assert.AreEqual(5, folders.Count);
    }

    [Test]
    public void RecursiveSearchPattern()
    {
        List<string> folders = DirectoryScanner.GetFolders(folderPath,
            new DirectoryScannerConfig(searchPattern) { ExcludeHiddenFolders = false, Recursive = true });
        Assert.AreEqual(2, folders.Count);
    }

    [Test]
    public void RecursiveExcludeHiddenFolders()
    {
        List<string> folders = DirectoryScanner.GetFolders(folderPath,
            new DirectoryScannerConfig() { ExcludeHiddenFolders = true, Recursive = true });
        Assert.AreEqual(4, folders.Count);
    }
}
