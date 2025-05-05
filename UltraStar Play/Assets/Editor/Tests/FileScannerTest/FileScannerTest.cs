using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class FileScannerTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/FileScannerTest/";

    private static readonly string txtFilePattern = "*.txt";
    private static readonly string txtMetaFilePattern = "*.txt.meta";
    private static readonly List<string> patterns = new()
    {
        txtFilePattern,
        txtMetaFilePattern,
    };

    [Test]
    public void NonRecursive()
    {
        List<string> files = FileScanner.GetFiles(folderPath,
            new FileScannerConfig(txtFilePattern) { ExcludeHiddenFiles = false, ExcludeHiddenFolders = false, Recursive = false });
        Assert.AreEqual(2, files.Count);
    }

    [Test]
    public void NonRecursiveMultiplePatterns()
    {
        List<string> files = FileScanner.GetFiles(folderPath,
            new FileScannerConfig(patterns) { ExcludeHiddenFiles = false, ExcludeHiddenFolders = false, Recursive = false });
        Assert.AreEqual(4, files.Count);
    }

    [Test]
    public void Recursive()
    {
        List<string> files = FileScanner.GetFiles(folderPath,
            new FileScannerConfig(txtFilePattern) { ExcludeHiddenFiles = false, ExcludeHiddenFolders = false, Recursive = true });
        Assert.AreEqual(6, files.Count);
    }

    [Test]
    public void RecursiveMultiplePatterns()
    {
        List<string> files = FileScanner.GetFiles(folderPath,
            new FileScannerConfig(patterns) { ExcludeHiddenFiles = false, ExcludeHiddenFolders = false, Recursive = true });
        Assert.AreEqual(12, files.Count);
    }

    [Test]
    public void RecursiveExcludeHiddenFolders()
    {
        List<string> files = FileScanner.GetFiles(folderPath,
            new FileScannerConfig(txtFilePattern) { ExcludeHiddenFiles = false, ExcludeHiddenFolders = true, Recursive = true });
        Assert.AreEqual(4, files.Count);
    }

    [Test]
    public void RecursiveExcludeHiddenFiles()
    {
        List<string> files = FileScanner.GetFiles(folderPath,
            new FileScannerConfig(txtFilePattern) { ExcludeHiddenFiles = true, ExcludeHiddenFolders = false, Recursive = true });
        Assert.AreEqual(5, files.Count);
    }
}
