using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class UltraStarSongFormatTests
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs/";

    [Test]
    public void MissingTagNameTest()
    {
        Translation.InitTranslationConfig();
        SongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-MissingTagName.txt", out List<SongIssue> songIssues, null, true);
        Assert.That(songIssues.AnyMatch(songIssue => songIssue.Message.Value.ToLowerInvariant().Contains("invalid formatting")));
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
    public void V100DeprecatedFields()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-v1.0.0.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual(songMeta.Version.EnumValue, EUltraStarSongFormatVersion.V100);
        Assert.AreEqual(songMeta.Audio, "TestSong.ogg");
        Assert.AreEqual(songMeta.TxtFileMedleyStartBeat, 4);
        Assert.AreEqual(songMeta.TxtFileMedleyEndBeat, 12);
    }

    [Test]
    public void V100LoadInconsistentTimeUnits()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-v1.0.0.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual(EUltraStarSongFormatVersion.V100, songMeta.Version.EnumValue);
        Assert.AreEqual(1000, songMeta.GapInMillis);
        Assert.AreEqual(3, songMeta.TxtFileVideoGapInSeconds);
        Assert.AreEqual(3000, songMeta.VideoGapInMillis);
        Assert.AreEqual(4, songMeta.TxtFileStartInSeconds);
        Assert.AreEqual(4000, songMeta.StartInMillis);
        Assert.AreEqual(5000, songMeta.EndInMillis);
        Assert.AreEqual(6, songMeta.TxtFilePreviewStartInSeconds);
        Assert.AreEqual(6000, songMeta.PreviewStartInMillis);
        Assert.AreEqual(7, songMeta.TxtFilePreviewEndInSeconds);
        Assert.AreEqual(7000, songMeta.PreviewEndInMillis);
        Assert.AreEqual(4, songMeta.TxtFileMedleyStartBeat);
        Assert.AreEqual(12, songMeta.TxtFileMedleyEndBeat);
    }

    [Test]
    public void V200LoadConsistentMillisecondsTimeUnit()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-v2.0.0.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual(EUltraStarSongFormatVersion.V200, songMeta.Version.EnumValue);
        Assert.AreEqual(1000, songMeta.GapInMillis);
        Assert.AreEqual(3000, songMeta.VideoGapInMillis);
        Assert.AreEqual(4000, songMeta.StartInMillis);
        Assert.AreEqual(5000, songMeta.EndInMillis);
        Assert.AreEqual(6000, songMeta.PreviewStartInMillis);
        Assert.AreEqual(7000, songMeta.PreviewEndInMillis);
        Assert.AreEqual(8000, songMeta.MedleyStartInMillis);
        Assert.AreEqual(9000, songMeta.MedleyEndInMillis);
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
        Assert.AreEqual(expectedVersionString, songMeta.Version.StringValue);
        Assert.AreEqual(expectedVersionEnum, songMeta.Version.EnumValue);
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
    public void NegativeNoteValues()
    {
        LogAssert.ignoreFailingMessages = true;

        SongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + "TestSong-NegativeNoteValues.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.IsTrue(SongMetaUtils.GetAllNotes(songMeta).Count > 0);
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

    [Test]
    public void CopyValuesFromUltraStarSongMetaToUltraStarSongMetaTest()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties-TestSong.txt";
        UltraStarSongMeta originalSongMeta = UltraStarSongParser.ParseFile(originalFilePath, out List<SongIssue> _);
        UltraStarSongMeta copiedSongMeta = new(originalSongMeta);
        string originalJson = JsonConverter.ToJson(originalSongMeta);
        string copyJson = JsonConverter.ToJson(copiedSongMeta);
        Assert.AreEqual(originalJson, copyJson);
    }

    [Test]
    public void CopyValuesFromSongMetaToUltraStarSongMetaTest()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties-TestSong.txt";
        SongMeta originalSongMeta = UltraStarSongParser.ParseFile(originalFilePath, out List<SongIssue> _);

        SongMeta copiedSongMeta = new UltraStarSongMeta();
        copiedSongMeta.CopyValues(originalSongMeta);

        SongMetaAssertUtils.AssertSongMetasAreEqual(originalSongMeta, copiedSongMeta);
    }

    [Test]
    [TestCase("1.0.0")]
    [TestCase("1.1.0")]
    [TestCase("1.2.0")]
    [TestCase("2.0.0")]
    public void LoadAndSaveSongDoesNotChangeFieldsOfUltraStarSongMeta(string formatVersion)
    {
        LoadAndSaveSongDoesNotChangeFieldsOfSongMeta(
            formatVersion,
            path => UltraStarSongParser.ParseFile(path, out List<SongIssue> _));
    }

    [Test]
    [TestCase("1.0.0")]
    [TestCase("1.1.0")]
    [TestCase("1.2.0")]
    [TestCase("2.0.0")]
    public void LoadAndSaveSongDoesNotChangeFieldsOfLazyLoadedFromFileSongMeta(string formatVersion)
    {
        LoadAndSaveSongDoesNotChangeFieldsOfSongMeta(
            formatVersion,
            path => new LazyLoadedFromFileSongMeta(path));
    }

    private static void LoadAndSaveSongDoesNotChangeFieldsOfSongMeta(string formatVersion, Func<string, SongMeta> loadSongMeta)
    {
        // Load file content with modified formatVersion
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties-TestSong.txt";
        string originalFileContent = File.ReadAllText(originalFilePath);
        string originalFileContentWithModifiedVersion = ReplaceHeaderField(originalFileContent, "VERSION", formatVersion);

        // Load song
        string copiedOriginalFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-TestSong-Original.txt";
        File.WriteAllText(copiedOriginalFilePath, originalFileContentWithModifiedVersion);
        SongMeta originalSongMeta = loadSongMeta(copiedOriginalFilePath);

        // Check loaded formatVersion matches modified formatVersion
        Assert.AreEqual(formatVersion, originalSongMeta.Version.StringValue);

        // Save song and check that no properties changed
        string savedFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-TestSong-Saved.txt";
        UltraStarFormatWriter.WriteFile(savedFilePath, originalSongMeta, originalSongMeta.Version);

        SongMeta savedSongMeta = loadSongMeta(savedFilePath);

        SongMetaAssertUtils.AssertSongMetasAreEqual(originalSongMeta, savedSongMeta);
    }

    private static string ReplaceHeaderField(string originalFileContent, string headerName, string newValue)
    {
        return Regex.Replace(originalFileContent, $"#{headerName}:.+", $"#{headerName}:{newValue}");
    }
}
