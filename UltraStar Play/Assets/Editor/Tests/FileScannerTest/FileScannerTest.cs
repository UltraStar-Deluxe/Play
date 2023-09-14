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
    public void NonRecursiveTest()
    {
        FileScanner fileScanner = new(txtFilePattern, false, false);
        List<string> files = fileScanner.GetFiles(folderPath, false);
        Assert.AreEqual(2, files.Count);
    }

    [Test]
    public void NonRecursiveMultiplePatternsTest()
    {
        FileScanner fileScanner = new(patterns, false, false);
        List<string> files = fileScanner.GetFiles(folderPath, false);
        Assert.AreEqual(4, files.Count);
    }

    [Test]
    public void RecursiveTest()
    {
        FileScanner fileScanner = new(txtFilePattern, false, false);
        List<string> files = fileScanner.GetFiles(folderPath, true);
        Assert.AreEqual(6, files.Count);
    }

    [Test]
    public void RecursiveMultiplePatternsTest()
    {
        FileScanner fileScanner = new(patterns, false, false);
        List<string> files = fileScanner.GetFiles(folderPath, true);
        Assert.AreEqual(12, files.Count);
    }

    [Test]
    public void RecursiveExcludeHiddenFoldersTest()
    {
        FileScanner fileScanner = new(txtFilePattern, true, false);
        List<string> files = fileScanner.GetFiles(folderPath, true);
        Assert.AreEqual(4, files.Count);
    }

    [Test]
    public void RecursiveExcludeHiddenFilesTest()
    {
        FileScanner fileScanner = new(txtFilePattern, false, true);
        List<string> files = fileScanner.GetFiles(folderPath, true);
        Assert.AreEqual(5, files.Count);
    }
}
