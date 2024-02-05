using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ComputeSongIdTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/TestSongs";

    [Test]
    public void ComputeScoreRelevantSongHashTest()
    {
        string originalFilePath = $"{folderPath}/ScoreRelevantSongHash-TestSong.txt";
        UltraStarSongMeta songMeta = LoadSong(originalFilePath);
        string computeScoreRelevantSongHash = SongMetaUtils.ComputeScoreRelevantSongHash(songMeta);

        Assert.AreEqual("881be91e5214e6b381d467595789c5a9", computeScoreRelevantSongHash,
            "ScoreRelevantSongHash calculation changed. " +
            "If this was intended then make sure to update the score database version and compatibility.");
    }

    private void AssertSongMetasAreEqual(UltraStarSongMeta expected, UltraStarSongMeta actual)
    {
        Assert.AreEqual(expected.Artist, actual.Artist);
        Assert.AreEqual(expected.Background, actual.Background);
        Assert.AreEqual(expected.BeatsPerMinute, actual.BeatsPerMinute);
        Assert.AreEqual(expected.Cover, actual.Cover);
        Assert.AreEqual(expected.Edition, actual.Edition);
        Assert.AreEqual(expected.EndInMillis, actual.EndInMillis);
        Assert.AreEqual(expected.GapInMillis, actual.GapInMillis);
        Assert.AreEqual(expected.Genre, actual.Genre);
        Assert.AreEqual(expected.InstrumentalAudio, actual.InstrumentalAudio);
        Assert.AreEqual(expected.Language, actual.Language);
        Assert.AreEqual(expected.Audio, actual.Audio);
        Assert.AreEqual(expected.PreviewEndInMillis, actual.PreviewEndInMillis);
        Assert.AreEqual(expected.MedleyEndInMillis, actual.MedleyEndInMillis);
        Assert.AreEqual(expected.MedleyStartInMillis, actual.MedleyStartInMillis);
        Assert.AreEqual(expected.PreviewStartInMillis, actual.PreviewStartInMillis);
        Assert.AreEqual(expected.StartInMillis, actual.StartInMillis);
        Assert.AreEqual(expected.Title, actual.Title);
        Assert.AreEqual(expected.Video, actual.Video);
        Assert.AreEqual(expected.VideoGapInMillis, actual.VideoGapInMillis);
        Assert.AreEqual(expected.VocalsAudio, actual.VocalsAudio);
        Assert.AreEqual(expected.Year, actual.Year);
        Assert.AreEqual(expected.Website, actual.Website);

        Assert.AreEqual("First Vocals", expected.GetVoiceDisplayName(EVoiceId.P1));
        Assert.AreEqual("Second Vocals", expected.GetVoiceDisplayName(EVoiceId.P2));
        Assert.AreEqual("First Vocals", actual.GetVoiceDisplayName(EVoiceId.P1));
        Assert.AreEqual("Second Vocals", actual.GetVoiceDisplayName(EVoiceId.P2));

        Assert.AreEqual("42,5", expected.GetAdditionalHeaderEntry("NUMBERWITHCOMMA"));
        Assert.AreEqual("43.2", expected.GetAdditionalHeaderEntry("NUMBERWITHDOT"));
        Assert.AreEqual("SomeOtherValue", expected.GetAdditionalHeaderEntry("UNSUPPORTEDFIELD"));
        Assert.IsTrue(expected.AdditionalHeaderEntries.SequenceEqual(actual.AdditionalHeaderEntries), "UnknownHeaderEntries not equal");

        Assert.AreEqual(2, expected.VoiceCount);
        Assert.AreEqual(expected.VoiceCount, actual.VoiceCount);
        Assert.IsNotEmpty(SongMetaUtils.GetLyrics(expected, EVoiceId.P1));
        Assert.IsNotEmpty(SongMetaUtils.GetLyrics(expected, EVoiceId.P2));
        Assert.AreEqual(SongMetaUtils.GetLyrics(expected, EVoiceId.P1), SongMetaUtils.GetLyrics(actual, EVoiceId.P1));
        Assert.AreEqual(SongMetaUtils.GetLyrics(expected, EVoiceId.P2), SongMetaUtils.GetLyrics(actual, EVoiceId.P2));

        // Remove FileInfo from data structure such that both should serialize to same JSON
        FileInfo originalSongMetaFileInfo = expected.FileInfo;
        expected.SetFileInfo((FileInfo)null);

        FileInfo savedSongMetaFileInfo = actual.FileInfo;
        actual.SetFileInfo((FileInfo)null);
        string originalSongJson = JsonConverter.ToJson(expected);
        string savedSongJson = JsonConverter.ToJson(actual);
        Assert.AreEqual(originalSongJson, savedSongJson);

        // Restore FileInfo
        expected.SetFileInfo(originalSongMetaFileInfo);
        actual.SetFileInfo(savedSongMetaFileInfo);
    }

    private UltraStarSongMeta LoadSong(string path)
    {
        return UltraStarSongParser.ParseFile(path, out List<SongIssue> songIssues, null, true);
    }
}
