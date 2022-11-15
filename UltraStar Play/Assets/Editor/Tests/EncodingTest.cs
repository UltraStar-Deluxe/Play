using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EncodingTest
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs/";
    private static readonly string testSongTitle = "TestTitle___ä___ß___'___Ж";

    [Test]
    public void TestUtf8Bom()
    {
        TestFile("TestSong-UTF8-BOM.txt");
    }

    [Test]
    public void TestUtf8NoBom()
    {
        TestFile("TestSong-UTF8-NoBOM.txt");
    }

    [Test]
    public void TestUtf16BeBom()
    {
        TestFile("TestSong-UTF16-BE-BOM.txt");
    }

    [Test]
    public void TestUtf16LeBom()
    {
        TestFile("TestSong-UTF16-LE-BOM.txt");
    }

    [Test]
    public void TestAscii()
    {
        // The file contains non-ASCII characters. Thus, the test should fail with an exception.
        Assert.Catch(delegate { TestFile("TestSong-ASCII.txt"); });
    }

    private void TestFile(string fileName)
    {
        string filePath = folderPath + fileName;
        SongMeta songMeta = SongMetaBuilder.ParseFile(filePath, out List<SongIssue> _);
        Assert.AreEqual(testSongTitle, songMeta.Title);
    }
}
