using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class UltraStarSongFormatTest
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs";

    [Test]
    public void ShouldHandleNoteWithoutLyricsAndMissingLastSeparator()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/NoteWithoutLyricsAndMissingLastSeparator.txt", out List<SongIssue> _);
        Assert.IsNotNull(songMeta);
        List<Note> notes = SongMetaUtils.GetAllNotes(songMeta);
        Assert.IsTrue(notes.Count == 6);
        Assert.IsTrue(notes[1].Text.IsNullOrEmpty());
    }

    [Test]
    public void ShouldAddPhraseEndBeatIfNotSameAsMaxBeat()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/WithPhraseEndBeat.txt", out List<SongIssue> _);
        string txt = UltraStarFormatWriter.ToUltraStarSongFormat(songMeta);
        Assert.IsTrue(Regex.IsMatch(txt, @"- \d"));
    }

    [Test]
    [Ignore("Linebreak timing could be optional but is required by some other tools, https://github.com/UltraStar-Deluxe/format/issues/64")]
    public void ShouldNotAddPhraseEndBeatIfSameAsMaxBeat()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/WithoutPhraseEndBeat.txt", out List<SongIssue> _);
        string txt = UltraStarFormatWriter.ToUltraStarSongFormat(songMeta);
        Assert.IsFalse(Regex.IsMatch(txt, @"- \d"));
        Assert.IsTrue(txt.Contains("-"));
    }

    [Test]
    public void ShouldNotContainP1InSoloSong()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/Solo.txt", out List<SongIssue> _);
        string txt = UltraStarFormatWriter.ToUltraStarSongFormat(songMeta);
        // P1 is optional when only having one voice
        Assert.IsFalse(txt.Contains("P1"));
    }

    [Test]
    public void ShouldContainP1AndP2InDuetSong()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/Duet.txt", out List<SongIssue> _);
        string txt = UltraStarFormatWriter.ToUltraStarSongFormat(songMeta);
        Assert.IsTrue(txt.Contains("P1"));
        Assert.IsTrue(txt.Contains("P2"));
    }

    [Test]
    public void ShouldHandleNoSpaceAfterNoteType()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/NoSpaceAfterNoteType.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        Assert.IsTrue(SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1).ToLowerInvariant().Contains("hello"));
        Assert.AreEqual(ENoteType.Normal, SongMetaUtils.GetAllNotes(songMeta)[0].Type);
        Assert.AreEqual(ENoteType.Golden, SongMetaUtils.GetAllNotes(songMeta)[1].Type);
    }

    [Test]
    public void ShouldHandleMultipleSpacesAsNoteSeparator()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/MultipleSpacesAsNoteSeparator.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        // P1 has space at start of word
        Assert.IsTrue(SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1).ToLowerInvariant().Contains("hello world~"));
        // P2 has space at end of word
        Assert.IsTrue(SongMetaUtils.GetLyrics(songMeta, EVoiceId.P2).ToLowerInvariant().Contains("hello world~"));
    }

    [Test]
    [TestCase("LyricsSpaceAtTheStart.txt")]
    [TestCase("LyricsSpaceAtTheEnd.txt")]
    public void ShouldHandLyricsSpace(string txtFileName)
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/{txtFileName}", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        string lyrics = SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1);
        Assert.AreEqual("hello world!", lyrics.ToLowerInvariant().Trim());
    }

    [Test]
    public void ShouldHandleMissingTagName()
    {
        Translation.InitTranslationConfig();
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/MissingTagName.txt", out List<SongIssue> songIssues);
        Assert.That(songIssues.AnyMatch(songIssue => songIssue.Message.Value.ToLowerInvariant().Contains("invalid formatting")));
        Assert.NotNull(songMeta);
    }

    [Test]
    public void ShouldHandleMissingTagValue()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/MissingTagValue.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        Assert.IsEmpty(songMeta.Language);
        Assert.AreEqual(0, songMeta.Year);
    }

    [Test]
    public void ShouldHandleV100DeprecatedFields()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/v1.0.0.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        Assert.AreEqual(songMeta.Version.EnumValue, EUltraStarSongFormatVersion.V100);
        Assert.AreEqual(songMeta.Audio, "TestSong.ogg");
        Assert.AreEqual(songMeta.TxtFileMedleyStartBeat, 4);
        Assert.AreEqual(songMeta.TxtFileMedleyEndBeat, 12);
    }

    [Test]
    public void ShouldHandleV100InconsistentTimeUnits()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/v1.0.0.txt", out List<SongIssue> _);
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
    public void ShouldHandleV200ConsistentMillisecondsTimeUnit()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/v2.0.0.txt", out List<SongIssue> _);
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
    [TestCase("InvalidVersion", "InvalidVersion", EUltraStarSongFormatVersion.Unknown)]
    public void VersionShouldBeParsed(string inputVersionString, string expectedVersionString, EUltraStarSongFormatVersion expectedVersionEnum)
    {
        string songFileContentWithVersionPlaceholder = File.ReadAllText($"{folderPath}/ParseVersion.txt");
        string songFileContentWithoutPlaceholder = songFileContentWithVersionPlaceholder.Replace("VERSION_PLACEHOLDER", inputVersionString);
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseString(songFileContentWithoutPlaceholder, out List<SongIssue> songIssues, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual(expectedVersionString, songMeta.Version.StringValue);
        Assert.AreEqual(expectedVersionEnum, songMeta.Version.EnumValue);
    }

    [Test]
    public void ShouldIgnoreSpaceAroundTagNameAndValue()
    {
        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/SpaceAroundTagNameAndValue.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        Assert.AreEqual("English", songMeta.Language);
        Assert.AreEqual(2022, songMeta.Year);
    }

    [Test]
    public void ShouldIgnoreNegativeNoteValues()
    {
        LogAssert.ignoreFailingMessages = true;

        SongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/NegativeNoteValues.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        Assert.IsTrue(SongMetaUtils.GetAllNotes(songMeta).Count > 0);
    }

    [Test]
    public void ShouldIgnoreSpaceAroundNumber()
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile($"{folderPath}/SpaceAroundNumber.txt", out List<SongIssue> _);
        Assert.NotNull(songMeta);
        Assert.AreEqual(200, songMeta.TxtFileBpm);
        Assert.AreEqual(0.12f, songMeta.GapInMillis, 0.001f);
        Assert.AreEqual(2022, songMeta.Year);
    }

    [Test]
    public void CopyValuesFromUltraStarSongMetaToUltraStarSongMetaShouldNotChangeFields()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties.txt";
        UltraStarSongMeta originalSongMeta = UltraStarSongParser.ParseFile(originalFilePath, out List<SongIssue> _);
        UltraStarSongMeta copiedSongMeta = new(originalSongMeta);
        string originalJson = JsonConverter.ToJson(originalSongMeta);
        string copyJson = JsonConverter.ToJson(copiedSongMeta);
        Assert.AreEqual(originalJson, copyJson);
    }

    [Test]
    public void CopyValuesFromSongMetaToUltraStarSongMetaShouldNotChangeFields()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties.txt";
        SongMeta originalSongMeta = UltraStarSongParser.ParseFile(originalFilePath, out List<SongIssue> _);

        SongMeta copiedSongMeta = new UltraStarSongMeta();
        copiedSongMeta.CopyValues(originalSongMeta);

        AssertSongMetaFields(copiedSongMeta);
        SongMetaAssertUtils.AssertSongMetasAreEqual(originalSongMeta, copiedSongMeta);
    }

    [Test]
    [TestCase("1.0.0")]
    [TestCase("1.1.0")]
    [TestCase("1.2.0")]
    [TestCase("2.0.0")]
    public void LoadAndSaveSongShouldNotChangeFieldsOfUltraStarSongMeta(string formatVersion)
    {
        LoadAndSaveSongShouldNotChangeFieldsOfSongMeta(
            formatVersion,
            path => UltraStarSongParser.ParseFile(path, out List<SongIssue> _));
    }

    [Test]
    [TestCase("1.0.0")]
    [TestCase("1.1.0")]
    [TestCase("1.2.0")]
    [TestCase("2.0.0")]
    public void LoadAndSaveSongShouldNotChangeFieldsOfLazyLoadedFromFileSongMeta(string formatVersion)
    {
        LoadAndSaveSongShouldNotChangeFieldsOfSongMeta(
            formatVersion,
            path => new LazyLoadedFromFileSongMeta(path));
    }

    private static void LoadAndSaveSongShouldNotChangeFieldsOfSongMeta(string formatVersion, Func<string, SongMeta> loadSongMeta)
    {
        // Load file content with modified formatVersion
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties.txt";
        string originalFileContent = File.ReadAllText(originalFilePath);
        string originalFileContentWithModifiedVersion = ReplaceHeaderField(originalFileContent, "VERSION", formatVersion);

        // Load song
        string copiedOriginalFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-Original.txt";
        File.WriteAllText(copiedOriginalFilePath, originalFileContentWithModifiedVersion);
        SongMeta originalSongMeta = loadSongMeta(copiedOriginalFilePath);

        // Check loaded formatVersion matches modified formatVersion
        Assert.AreEqual(formatVersion, originalSongMeta.Version.StringValue);

        // Save song and check that no properties changed
        string savedFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-Saved.txt";
        UltraStarFormatWriter.WriteFile(savedFilePath, originalSongMeta, originalSongMeta.Version);

        SongMeta savedSongMeta = loadSongMeta(savedFilePath);

        AssertSongMetaFields(savedSongMeta);
        SongMetaAssertUtils.AssertSongMetasAreEqual(originalSongMeta, savedSongMeta);
    }

    private static string ReplaceHeaderField(string originalFileContent, string headerName, string newValue)
    {
        return Regex.Replace(originalFileContent, $"#{headerName}:.+", $"#{headerName}:{newValue}");
    }

    private static void AssertSongMetaFields(SongMeta actual)
    {
        Assert.AreEqual("First Vocals", actual.GetVoiceDisplayName(EVoiceId.P1));
        Assert.AreEqual("Second Vocals", actual.GetVoiceDisplayName(EVoiceId.P2));

        Assert.AreEqual("42,5", actual.GetAdditionalHeaderEntry("NUMBERWITHCOMMA"));
        Assert.AreEqual("43.2", actual.GetAdditionalHeaderEntry("NUMBERWITHDOT"));
        Assert.AreEqual("SomeOtherValue", actual.GetAdditionalHeaderEntry("UNSUPPORTEDFIELD"));

        Assert.AreEqual(2, actual.VoiceCount);
        Assert.IsNotEmpty(SongMetaUtils.GetLyrics(actual, EVoiceId.P1));
        Assert.IsNotEmpty(SongMetaUtils.GetLyrics(actual, EVoiceId.P2));
    }
}
