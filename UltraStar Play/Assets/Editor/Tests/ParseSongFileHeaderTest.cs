using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class ParseSongFileHeaderTest
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs/";

    [Test]
    public void MissingTagNameTest()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-MissingTagName.txt", out List<SongIssue> songIssues, null, true);
        Assert.That(songIssues.AnyMatch(songIssue => songIssue.Message.ToLowerInvariant().Contains("invalid formatting of header field")));
        Assert.NotNull(songMeta);
    }

    [Test]
    public void MissingTagValueTest()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-MissingTagValue.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.IsEmpty(songMeta.Language);
        Assert.AreEqual(0, songMeta.Year);
    }

    [Test]
    [TestCase("1.0.0", "1.0.0", EUltraStarSongFormatVersion.V100)]
    [TestCase("v1.0.0", "1.0.0", EUltraStarSongFormatVersion.V100)]
    [TestCase("1.1.0", "1.1.0", EUltraStarSongFormatVersion.V110)]
    [TestCase("1.2.0", "1.2.0", EUltraStarSongFormatVersion.V120)]
    [TestCase("V2.0.0", "2.0.0", EUltraStarSongFormatVersion.V200)]
    [TestCase("", "1.0.0", EUltraStarSongFormatVersion.V100)]
    [TestCase(null, "1.0.0", EUltraStarSongFormatVersion.V100)]
    [TestCase("InvalidVersion", "InvalidVersion", EUltraStarSongFormatVersion.Unknown)]
    public void VersionShouldBeParsed(string inputVersionString, string expectedVersionString, EUltraStarSongFormatVersion expectedVersionEnum)
    {
        string songFileContentWithVersionPlaceholder = File.ReadAllText(folderPath + "TestSong-ParseVersion.txt");
        string songFileContentWithoutPlaceholder = songFileContentWithVersionPlaceholder.Replace("VERSION_PLACEHOLDER", inputVersionString);
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseString(songFileContentWithoutPlaceholder, out List<SongIssue> songIssues, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual(songMeta.Version.StringValue, expectedVersionString);
        Assert.AreEqual(songMeta.Version.EnumValue, expectedVersionEnum);
    }

    [Test]
    public void SpaceAroundTagNameAndValueTest()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-SpaceAroundTagNameAndValue.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual("English", songMeta.Language);
        Assert.AreEqual(2022, songMeta.Year);
    }

    [Test]
    public void SpaceAroundNumberTest()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-SpaceAroundNumber.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual(200, songMeta.TxtFileBpm);
        Assert.AreEqual(0.12f, songMeta.GapInMillis, 0.001f);
        Assert.AreEqual(2022, songMeta.Year);
    }
}
