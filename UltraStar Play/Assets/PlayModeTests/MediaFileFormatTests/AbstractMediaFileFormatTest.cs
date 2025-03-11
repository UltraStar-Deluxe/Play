using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

public abstract class AbstractMediaFileFormatTest : AbstractPlayModeTest
{
    protected static readonly string fileFormatTestFolderPath = $"{Application.dataPath}/PlayModeTests/MediaFileFormatTests";
    protected static readonly string mediaFileFormatTestFolderPath = $"{fileFormatTestFolderPath}/MediaFileFormatTestSongs";

    protected const double DefaultTargetDurationInMillis = 4000;
    private const double MaxDistanceToTargetDurationInMillis = 500;

    protected const long DefaultMaxWaitTimeInMillis = 5000;

    protected override string TestSceneName => "MediaFileFormatTestScene";

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    protected SongAudioPlayer songAudioPlayer;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    protected SongVideoPlayer songVideoPlayer;

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        base.ConfigureTestSettings(settings);
        settings.LogVlcOutput = true;
    }

    protected async Awaitable SongAudioPlayerShouldLoadFileAsync(
        string txtFilePath,
        double targetDurationInMillis = DefaultTargetDurationInMillis,
        long maxWaitTimeInMillis = DefaultMaxWaitTimeInMillis)
    {
        await SongMediaPlayerShouldLoadFileAsync(songAudioPlayer, txtFilePath, targetDurationInMillis, maxWaitTimeInMillis);
    }

    protected async Awaitable SongVideoPlayerShouldLoadFileAsync(
        string txtFilePath,
        double targetDurationInMillis = DefaultTargetDurationInMillis,
        long maxWaitTimeInMillis = DefaultMaxWaitTimeInMillis)
    {
        // The SongVideoPlayer requires a running SongAudioPlayer.
        // For example for time sync and to reuse video if possible (depending on VideoSupportProvider).
        await SongMediaPlayerShouldLoadFileAsync(songAudioPlayer, txtFilePath, targetDurationInMillis, maxWaitTimeInMillis);
        await SongMediaPlayerShouldLoadFileAsync(songVideoPlayer, txtFilePath, targetDurationInMillis, maxWaitTimeInMillis);
    }

    private async Awaitable SongMediaPlayerShouldLoadFileAsync<T>(
        ISongMediaPlayer<T> songMediaPlayer,
        string txtFilePath,
        double targetDurationInMillis = DefaultTargetDurationInMillis,
        long maxWaitTimeInMillis = DefaultMaxWaitTimeInMillis) where T : ISongMediaLoadedEvent
    {
        LogAssertUtils.IgnoreFailingMessages();

        string songFilePath = GetSongMetaFilePath(txtFilePath);
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();

        try
        {
            SongMeta songMeta = LoadSongMeta(songFilePath);
            T evt = await songMediaPlayer.LoadAndPlayAsync(songMeta);

            double durationInMillis = songMediaPlayer.DurationInMillis;
            if (durationInMillis <= 0)
            {
                Assert.Fail($"Failed to load, duration is 0.");
            }

            if (Math.Abs(durationInMillis - targetDurationInMillis) > MaxDistanceToTargetDurationInMillis)
            {
                Assert.Fail($"Expected duration near {targetDurationInMillis} ms, but was {durationInMillis} ms.");
            }

            if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis))
            {
                Assert.Fail($"Failed to load audio after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms. SongMeta: {JsonConverter.ToJson(songMeta)}");
            }

            Debug.Log($"Loaded successfully after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms, media duration: {songMediaPlayer.DurationInMillis} ms, mediaUri: '{evt.MediaUri}'");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    protected string GetSongMetaFilePath(string txtFilePath)
    {
        return txtFilePath.Contains("/")
            ? $"{fileFormatTestFolderPath}/{txtFilePath}"
            : $"{mediaFileFormatTestFolderPath}/{txtFilePath}";
    }

    protected SongMeta LoadSongMeta(string songFilePath)
    {
        UltraStarSongParserResult parserResult = UltraStarSongParser.ParseFile(songFilePath,
            new UltraStarSongParserConfig { Encoding = Encoding.UTF8, UseUniversalCharsetDetector = false });
        if (parserResult.SongMeta == null)
        {
            Assert.Fail($"Failed to load song from path '{songFilePath}'");
        }

        if (!parserResult.SongIssues.IsNullOrEmpty())
        {
            string songIssuesCsv = parserResult.SongIssues.Select(songIssue => songIssue.Message).JoinWith("\n    - ");
            Assert.Fail($"Issues found with song at path '{songFilePath}':\n    - {songIssuesCsv}");
        }

        return parserResult.SongMeta;
    }
}
