using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EncodingTest
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs/";

    [Test]
    public void TestUtf8Bom()
    {
        TestFile("TestSong-UTF8-BOM.txt", true);
        TestFile("TestSong-UTF8-BOM.txt", false);
    }

    [Test]
    public void TestUtf8NoBom()
    {
        TestFile("TestSong-UTF8-NoBOM.txt", true);
        TestFile("TestSong-UTF8-NoBOM.txt", false);
    }

    [Test]
    public void TestUtf16BeBom()
    {
        TestFile("TestSong-UTF16-BE-BOM.txt", true);
        TestFile("TestSong-UTF16-BE-BOM.txt", false);
    }

    [Test]
    public void TestUtf16LeBom()
    {
        TestFile("TestSong-UTF16-LE-BOM.txt", true);
        TestFile("TestSong-UTF16-LE-BOM.txt", false);
    }

    [Test]
    public void TestWindows1252()
    {
        // The file contains special characters and was saved in a non-Unicode encoding.
        // Thus, the test should fail with an exception when only detecting Unicode encodings.
        Assert.Catch(delegate { TestFile("TestSong-Windows1252.txt",
            false,
            "Käse und Gemüse",
            "Tränenüberströmt nach Fußmassage",
            new List<string> { "Süße", "Löwenbabys" }); });

        // It should work with the universal charset detector.
        TestFile("TestSong-Windows1252.txt",
            true,
            "Käse und Gemüse",
            "Tränenüberströmt nach Fußmassage",
            new List<string> { "Süße", "Löwenbabys" });
    }

    [Test]
    public void TestIso8859_1()
    {
        TestFile("TestSong-ISO-8859-1.txt",
            true,
            "Käse und Gemüse",
            "Tränenüberströmt nach Fußmassage",
            new List<string> { "Süße", "Löwenbabys" });
    }

    [Test]
    public void TestCp865()
    {
        // This encoding fails even with Universal Charset Detector
        Assert.Catch(delegate { TestFile("TestSong-cp865.txt",
                true,
                "å være midt i smørøyet",
                "det finnes ikke dårlig vær, bare dårlige klær"); });

        // It works when explicitly specifying the encoding in the txt file because the song parsing will consider this header field.
        TestFile("TestSong-cp865-explicit-encoding.txt",
            true,
            "å være midt i smørøyet",
            "det finnes ikke dårlig vær, bare dårlige klær");
    }

    [Test]
    public void TestKoi8_r()
    {
        TestFile("TestSong-koi8-r.txt",
            true,
            "Кириллица is Cyrillic",
            "SongTitle");
    }

    private void TestFile(
        string fileName,
        bool useUniversalCharsetDetector = true,
        string songArtist = "TestArtist",
        string songTitle = "TestTitle___ä___ß___'___Ж",
        List<string> wordsInLyrics = null)
    {
        string filePath = folderPath + fileName;
        SongMeta songMeta = SongMetaBuilder.ParseFile(filePath, out List<SongIssue> _, null, useUniversalCharsetDetector);
        Assert.AreEqual(songArtist, songMeta.Artist);
        Assert.AreEqual(songTitle, songMeta.Title);

        if (!wordsInLyrics.IsNullOrEmpty())
        {
            string lyrics = SongMetaUtils.GetLyrics(songMeta, Voice.firstVoiceName);
            wordsInLyrics.ForEach(word => Assert.IsTrue(lyrics.Contains(word), $"Lyrics did not contain the word '{word}'. Lyrics:\n{lyrics}"));
        }
    }
}
