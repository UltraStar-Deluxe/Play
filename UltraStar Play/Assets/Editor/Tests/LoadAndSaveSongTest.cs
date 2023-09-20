using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class LoadAndSaveSongTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/TestSongs";

    [Test]
    public void LoadAndSaveSongDoesNotChangeFields()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties-TestSong.txt";
        SongMeta originalSongMeta = LoadSong(originalFilePath);

        string savedFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-TestSong-Saved.txt";
        UltraStarFormatWriter.WriteFile(savedFilePath, originalSongMeta);

        SongMeta savedSongMeta = LoadSong(savedFilePath);

        Assert.AreEqual(originalSongMeta.Artist, savedSongMeta.Artist);
        Assert.AreEqual(originalSongMeta.Background, savedSongMeta.Background);
        Assert.AreEqual(originalSongMeta.Bpm, savedSongMeta.Bpm);
        Assert.AreEqual(originalSongMeta.Cover, savedSongMeta.Cover);
        Assert.AreEqual(originalSongMeta.Edition, savedSongMeta.Edition);
        Assert.AreEqual(originalSongMeta.End, savedSongMeta.End);
        Assert.AreEqual(originalSongMeta.Gap, savedSongMeta.Gap);
        Assert.AreEqual(originalSongMeta.Genre, savedSongMeta.Genre);
        Assert.AreEqual(originalSongMeta.InstrumentalAudio, savedSongMeta.InstrumentalAudio);
        Assert.AreEqual(originalSongMeta.Language, savedSongMeta.Language);
        Assert.AreEqual(originalSongMeta.Mp3, savedSongMeta.Mp3);
        Assert.AreEqual(originalSongMeta.MusicBrainzRecord, savedSongMeta.MusicBrainzRecord);
        Assert.AreEqual(originalSongMeta.PreviewEnd, savedSongMeta.PreviewEnd);
        Assert.AreEqual(originalSongMeta.MedleyEndBeat, savedSongMeta.MedleyEndBeat);
        Assert.AreEqual(originalSongMeta.MedleyStartBeat, savedSongMeta.MedleyStartBeat);
        Assert.AreEqual(originalSongMeta.PreviewStart, savedSongMeta.PreviewStart);
        Assert.AreEqual(originalSongMeta.Start, savedSongMeta.Start);
        Assert.AreEqual(originalSongMeta.Title, savedSongMeta.Title);
        Assert.AreEqual(originalSongMeta.Video, savedSongMeta.Video);
        Assert.AreEqual(originalSongMeta.VideoGap, savedSongMeta.VideoGap);
        Assert.AreEqual(originalSongMeta.VocalsAudio, savedSongMeta.VocalsAudio);
        Assert.AreEqual(originalSongMeta.Year, savedSongMeta.Year);

        Assert.AreEqual("First Vocals", originalSongMeta.GetVoiceDisplayName(Voice.firstVoiceId));
        Assert.AreEqual("Second Vocals", originalSongMeta.GetVoiceDisplayName(Voice.secondVoiceId));
        Assert.AreEqual("First Vocals", savedSongMeta.GetVoiceDisplayName(Voice.firstVoiceId));
        Assert.AreEqual("Second Vocals", savedSongMeta.GetVoiceDisplayName(Voice.secondVoiceId));

        Assert.AreEqual("42,5", originalSongMeta.GetUnknownHeaderEntry("NUMBERWITHCOMMA"));
        Assert.AreEqual("43.2", originalSongMeta.GetUnknownHeaderEntry("NUMBERWITHDOT"));
        Assert.AreEqual("SomeOtherValue", originalSongMeta.GetUnknownHeaderEntry("UNSUPPORTEDFIELD"));
        Assert.IsTrue(originalSongMeta.UnknownHeaderEntries.SequenceEqual(savedSongMeta.UnknownHeaderEntries), "UnknownHeaderEntries not equal");
    }

    private SongMeta LoadSong(string path)
    {
        return UltraStarSongParser.ParseSongFile(path, out List<SongIssue> songIssues, null, true);
    }
}
