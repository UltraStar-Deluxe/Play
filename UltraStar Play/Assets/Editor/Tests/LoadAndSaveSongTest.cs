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
        UltraStarSongMeta originalSongMeta = LoadSong(originalFilePath);

        string savedFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-TestSong-Saved.txt";
        UltraStarFormatWriter.WriteFile(savedFilePath, originalSongMeta);

        UltraStarSongMeta savedSongMeta = LoadSong(savedFilePath);

        Assert.AreEqual(originalSongMeta.Artist, savedSongMeta.Artist);
        Assert.AreEqual(originalSongMeta.Background, savedSongMeta.Background);
        Assert.AreEqual(originalSongMeta.BeatsPerMinute, savedSongMeta.BeatsPerMinute);
        Assert.AreEqual(originalSongMeta.Cover, savedSongMeta.Cover);
        Assert.AreEqual(originalSongMeta.Edition, savedSongMeta.Edition);
        Assert.AreEqual(originalSongMeta.EndInMillis, savedSongMeta.EndInMillis);
        Assert.AreEqual(originalSongMeta.GapInMillis, savedSongMeta.GapInMillis);
        Assert.AreEqual(originalSongMeta.Genre, savedSongMeta.Genre);
        Assert.AreEqual(originalSongMeta.InstrumentalAudio, savedSongMeta.InstrumentalAudio);
        Assert.AreEqual(originalSongMeta.Language, savedSongMeta.Language);
        Assert.AreEqual(originalSongMeta.Audio, savedSongMeta.Audio);
        Assert.AreEqual(originalSongMeta.MusicBrainzRecord, savedSongMeta.MusicBrainzRecord);
        Assert.AreEqual(originalSongMeta.MusicBrainzRelease, savedSongMeta.MusicBrainzRelease);
        Assert.AreEqual(originalSongMeta.MusicBrainzReleaseGroup, savedSongMeta.MusicBrainzReleaseGroup);
        Assert.AreEqual(originalSongMeta.MusicBrainzArtist, savedSongMeta.MusicBrainzArtist);
        Assert.AreEqual(originalSongMeta.PreviewEndInMillis, savedSongMeta.PreviewEndInMillis);
        Assert.AreEqual(originalSongMeta.MedleyEndInMillis, savedSongMeta.MedleyEndInMillis);
        Assert.AreEqual(originalSongMeta.MedleyStartInMillis, savedSongMeta.MedleyStartInMillis);
        Assert.AreEqual(originalSongMeta.PreviewStartInMillis, savedSongMeta.PreviewStartInMillis);
        Assert.AreEqual(originalSongMeta.StartInMillis, savedSongMeta.StartInMillis);
        Assert.AreEqual(originalSongMeta.Title, savedSongMeta.Title);
        Assert.AreEqual(originalSongMeta.Video, savedSongMeta.Video);
        Assert.AreEqual(originalSongMeta.VideoGapInMillis, savedSongMeta.VideoGapInMillis);
        Assert.AreEqual(originalSongMeta.VocalsAudio, savedSongMeta.VocalsAudio);
        Assert.AreEqual(originalSongMeta.Year, savedSongMeta.Year);

        Assert.AreEqual("First Vocals", originalSongMeta.GetVoiceDisplayName(EVoiceId.P1));
        Assert.AreEqual("Second Vocals", originalSongMeta.GetVoiceDisplayName(EVoiceId.P2));
        Assert.AreEqual("First Vocals", savedSongMeta.GetVoiceDisplayName(EVoiceId.P1));
        Assert.AreEqual("Second Vocals", savedSongMeta.GetVoiceDisplayName(EVoiceId.P2));

        Assert.AreEqual("42,5", originalSongMeta.GetAdditionalHeaderEntry("NUMBERWITHCOMMA"));
        Assert.AreEqual("43.2", originalSongMeta.GetAdditionalHeaderEntry("NUMBERWITHDOT"));
        Assert.AreEqual("SomeOtherValue", originalSongMeta.GetAdditionalHeaderEntry("UNSUPPORTEDFIELD"));
        Assert.IsTrue(originalSongMeta.AdditionalHeaderEntries.SequenceEqual(savedSongMeta.AdditionalHeaderEntries), "UnknownHeaderEntries not equal");
    }

    private UltraStarSongMeta LoadSong(string path)
    {
        return UltraStarSongParser.ParseFile(path, out List<SongIssue> songIssues, null, true);
    }
}
