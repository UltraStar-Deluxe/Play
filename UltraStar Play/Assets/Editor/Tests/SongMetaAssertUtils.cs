using System.IO;
using System.Linq;
using NUnit.Framework;

public static class SongMetaAssertUtils
{
    public static void AssertSongMetasAreEqual(SongMeta expected, SongMeta actual)
    {
        string expectedUltraStarTxt = UltraStarFormatWriter.ToUltraStarSongFormat(expected);
        string actualUltraStarTxt = UltraStarFormatWriter.ToUltraStarSongFormat(actual);
        File.WriteAllText($"{Path.GetTempPath()}/SongMetaAssertUtils-expected.txt", expectedUltraStarTxt);
        File.WriteAllText($"{Path.GetTempPath()}/SongMetaAssertUtils-actual.txt", actualUltraStarTxt);

        Assert.AreEqual(expected.Artist, actual.Artist);
        Assert.AreEqual(expected.Audio, actual.Audio);
        Assert.AreEqual(expected.AudioUrl, actual.AudioUrl);
        Assert.AreEqual(expected.Background, actual.Background);
        Assert.AreEqual(expected.BackgroundUrl, actual.BackgroundUrl);
        Assert.AreEqual(expected.BeatsPerMinute, actual.BeatsPerMinute);
        Assert.AreEqual(expected.Cover, actual.Cover);
        Assert.AreEqual(expected.CoverUrl, actual.CoverUrl);
        Assert.AreEqual(expected.Edition, actual.Edition);
        Assert.AreEqual(expected.EndInMillis, actual.EndInMillis);
        Assert.AreEqual(expected.GapInMillis, actual.GapInMillis);
        Assert.AreEqual(expected.Genre, actual.Genre);
        Assert.AreEqual(expected.Tag, actual.Tag);
        Assert.AreEqual(expected.InstrumentalAudio, actual.InstrumentalAudio);
        Assert.AreEqual(expected.InstrumentalAudioUrl, actual.InstrumentalAudioUrl);
        Assert.AreEqual(expected.Language, actual.Language);
        Assert.AreEqual(expected.MedleyEndInMillis, actual.MedleyEndInMillis);
        Assert.AreEqual(expected.MedleyStartInMillis, actual.MedleyStartInMillis);
        Assert.AreEqual(expected.PreviewEndInMillis, actual.PreviewEndInMillis);
        Assert.AreEqual(expected.PreviewStartInMillis, actual.PreviewStartInMillis);
        Assert.AreEqual(expected.StartInMillis, actual.StartInMillis);
        Assert.AreEqual(expected.Title, actual.Title);
        Assert.AreEqual(expected.Video, actual.Video);
        Assert.AreEqual(expected.VideoUrl, actual.VideoUrl);
        Assert.AreEqual(expected.VideoGapInMillis, actual.VideoGapInMillis);
        Assert.AreEqual(expected.VocalsAudio, actual.VocalsAudio);
        Assert.AreEqual(expected.VocalsAudioUrl, actual.VocalsAudioUrl);
        Assert.AreEqual(expected.Year, actual.Year);

        if (expected is UltraStarSongMeta expectedUltraStarSongMeta &&
            actual is UltraStarSongMeta actualUltraStarSongMeta)
        {
            Assert.AreEqual(expectedUltraStarSongMeta.Version, actualUltraStarSongMeta.Version);
        }

        Assert.AreEqual(expected.GetVoiceDisplayName(EVoiceId.P1), actual.GetVoiceDisplayName(EVoiceId.P1));
        Assert.AreEqual(expected.GetVoiceDisplayName(EVoiceId.P2), actual.GetVoiceDisplayName(EVoiceId.P2));

        Assert.IsTrue(expected.AdditionalHeaderEntries.SequenceEqual(actual.AdditionalHeaderEntries), "UnknownHeaderEntries not equal");

        Assert.AreEqual(expected.VoiceCount, actual.VoiceCount);
        Assert.AreEqual(SongMetaUtils.GetLyrics(expected, EVoiceId.P1), SongMetaUtils.GetLyrics(actual, EVoiceId.P1));
        Assert.AreEqual(SongMetaUtils.GetLyrics(expected, EVoiceId.P2), SongMetaUtils.GetLyrics(actual, EVoiceId.P2));

        // Compare without FileInfo such that both should serialize to same JSON
        string originalSongJson = ToJsonWithoutFileInfo(expected);
        string savedSongJson = ToJsonWithoutFileInfo(actual);
        Assert.AreEqual(originalSongJson, savedSongJson);
    }

    private static string ToJsonWithoutFileInfo(SongMeta songMeta)
    {
        FileInfo fileInfo = songMeta.FileInfo;
        songMeta.SetFileInfo((FileInfo)null);
        string json = JsonConverter.ToJson(songMeta);
        songMeta.SetFileInfo(fileInfo);
        return json;
    }
}
