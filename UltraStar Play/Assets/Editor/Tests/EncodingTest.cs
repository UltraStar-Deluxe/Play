using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EncodingTest
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs/";

    [Test]
    public void ShouldReturnCorrectEncoding()
    {
        Assert.AreEqual(EncodingUtils.GetEncoding("utf8"), Encoding.UTF8);
        Assert.AreEqual(EncodingUtils.GetEncoding("utf-8"), Encoding.UTF8);
        Assert.AreEqual(EncodingUtils.GetEncoding("utf32"), Encoding.UTF32);
        Assert.AreEqual(EncodingUtils.GetEncoding("utf-32"), Encoding.UTF32);
        Assert.AreEqual(EncodingUtils.GetEncoding("utf16"), Encoding.Unicode);
        Assert.AreEqual(EncodingUtils.GetEncoding("utf-16"), Encoding.Unicode);
        Assert.AreEqual(EncodingUtils.GetEncoding("ascii"), Encoding.ASCII);
        Assert.AreEqual(EncodingUtils.GetEncoding("ansi"), Encoding.GetEncoding("windows-1252"));
        Assert.AreEqual(EncodingUtils.GetEncoding("cp1252"), Encoding.GetEncoding("windows-1252"));
        Assert.AreEqual(EncodingUtils.GetEncoding("windows1252"), Encoding.GetEncoding("WINDOWS-1252"));
    }

    [Test]
    public void Utf8Bom()
    {
        ShouldDetectCorrectEncoding("UTF8-BOM.txt", true);
        ShouldDetectCorrectEncoding("UTF8-BOM.txt", false);
    }

    [Test]
    public void Utf8NoBom()
    {
        ShouldDetectCorrectEncoding("UTF8-NoBOM.txt", true);
        ShouldDetectCorrectEncoding("UTF8-NoBOM.txt", false);
    }

    [Test]
    public void Utf16BeBom()
    {
        ShouldDetectCorrectEncoding("UTF16-BE-BOM.txt", true);
        ShouldDetectCorrectEncoding("UTF16-BE-BOM.txt", false);
    }

    [Test]
    public void Utf16LeBom()
    {
        ShouldDetectCorrectEncoding("UTF16-LE-BOM.txt", true);
        ShouldDetectCorrectEncoding("UTF16-LE-BOM.txt", false);
    }

    [Test]
    public void Windows1252()
    {
        // The file contains special characters and was saved in a non-Unicode encoding.
        // Thus, the  should fail with an exception when only detecting Unicode encodings.
        Assert.Catch(delegate { ShouldDetectCorrectEncoding("Windows1252.txt",
            false,
            "Käse und Gemüse",
            "Tränenüberströmt nach Fußmassage",
            new List<string> { "Süße", "Löwenbabys" }); });

        // It should work with the universal charset detector.
        ShouldDetectCorrectEncoding("Windows1252.txt",
            true,
            "Käse und Gemüse",
            "Tränenüberströmt nach Fußmassage",
            new List<string> { "Süße", "Löwenbabys" });
    }

    [Test]
    public void Iso8859_1()
    {
        ShouldDetectCorrectEncoding("ISO-8859-1.txt",
            true,
            "Käse und Gemüse",
            "Tränenüberströmt nach Fußmassage",
            new List<string> { "Süße", "Löwenbabys" });
    }

    [Test]
    public void Cp865()
    {
        // This encoding fails even with Universal Charset Detector
        Assert.Catch(delegate { ShouldDetectCorrectEncoding("cp865.txt",
                true,
                "å være midt i smørøyet",
                "det finnes ikke dårlig vær, bare dårlige klær"); });

        // It works when explicitly specifying the encoding in the txt file because the song parsing will consider this header field.
        ShouldDetectCorrectEncoding("cp865-explicit-encoding.txt",
            true,
            "å være midt i smørøyet",
            "det finnes ikke dårlig vær, bare dårlige klær");
    }

    [Test]
    public void Koi8_r()
    {
        ShouldDetectCorrectEncoding("koi8-r.txt",
            true,
            "Кириллица is Cyrillic",
            "Я люблю тебя");
    }

    [Test]
    public void InvalidExplicitEncodingShouldThrowError()
    {
        LogAssert.Expect(LogType.Exception, new Regex(@".+'InvalidEncoding'.+"));
        LogAssert.Expect(LogType.Error, new Regex(@".+'InvalidEncoding'.+"));

        ShouldDetectCorrectEncoding("invalid-explicit-encoding.txt",
            true,
            "SongArtist",
            "SongTitle");
    }

    private void ShouldDetectCorrectEncoding(
        string fileName,
        bool useUniversalCharsetDetector = true,
        string songArtist = "TestArtist",
        string songTitle = "TestTitle___ä___ß___'___Ж",
        List<string> wordsInLyrics = null)
    {
        string filePath = folderPath + fileName;
        SongMeta songMeta = UltraStarSongParser.ParseFile(filePath, out List<SongIssue> _, null, useUniversalCharsetDetector);
        Assert.AreEqual(songArtist, songMeta.Artist);
        Assert.AreEqual(songTitle, songMeta.Title);

        if (!wordsInLyrics.IsNullOrEmpty())
        {
            string lyrics = SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1);
            wordsInLyrics.ForEach(word => Assert.IsTrue(lyrics.Contains(word), $"Lyrics did not contain the word '{word}'. Lyrics:\n{lyrics}"));
        }
    }
}
