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
    private const double MaxDistanteToTargetDurationInMillis = 500;

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
        settings.LogFfmpegOutput = true;
    }

    protected IEnumerator SongAudioPlayerShouldLoadFile(
        string txtFilePath,
        double targetDurationInMillis = DefaultTargetDurationInMillis,
        long maxWaitTimeInMillis = DefaultMaxWaitTimeInMillis)
    {
        yield return SongMediaPlayerShouldLoadFile(songAudioPlayer, txtFilePath, targetDurationInMillis, maxWaitTimeInMillis);
    }

    protected IEnumerator SongVideoPlayerShouldLoadFile(
        string txtFilePath,
        double targetDurationInMillis = DefaultTargetDurationInMillis,
        long maxWaitTimeInMillis = DefaultMaxWaitTimeInMillis)
    {
        // The SongVideoPlayer requires a running SongAudioPlayer.
        // For example for time sync and to reuse video if possible (depending on VideoSupportProvider).
        yield return SongMediaPlayerShouldLoadFile(songAudioPlayer, txtFilePath, targetDurationInMillis, maxWaitTimeInMillis);
        yield return SongMediaPlayerShouldLoadFile(songVideoPlayer, txtFilePath, targetDurationInMillis, maxWaitTimeInMillis);
    }

    private IEnumerator SongMediaPlayerShouldLoadFile<T>(
        ISongMediaPlayer<T> songMediaPlayer,
        string txtFilePath,
        double targetDurationInMillis = DefaultTargetDurationInMillis,
        long maxWaitTimeInMillis = DefaultMaxWaitTimeInMillis) where T : ISongMediaLoadedEvent
    {
        LogAssert.ignoreFailingMessages = true;

        string songFilePath = GetSongMetaFilePath(txtFilePath);
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        bool hasFailed = false;

        SongMeta songMeta = LoadSongMeta(songFilePath);
        songMediaPlayer.LoadAndPlayAsObservable(songMeta)
            .Select(evt =>
            {
                double durationInMillis = songMediaPlayer.DurationInMillis;
                if (durationInMillis <= 0)
                {
                    Assert.Fail($"Failed to load, duration is 0.");
                }

                if (Math.Abs(durationInMillis - targetDurationInMillis) > MaxDistanteToTargetDurationInMillis)
                {
                    Assert.Fail($"Expected duration near {targetDurationInMillis} ms, but was {durationInMillis} ms.");
                }

                return evt;
            })
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                hasFailed = true;
            })
            .Subscribe(evt =>
            {
                Debug.Log($"Loaded successfully after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms, media duration: {songMediaPlayer.DurationInMillis} ms, mediaUri: '{evt.MediaUri}'");
            });

        yield return new WaitUntil(() => songMediaPlayer.DurationInMillis > 0
                                         || hasFailed
                                         || TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis));

        if (hasFailed)
        {
            Assert.Fail("Test failed. Check log for details.");
        }
        else if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis))
        {
            Assert.Fail($"Failed to load audio after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms. SongMeta: {JsonConverter.ToJson(songMeta)}");
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
        SongMeta songMeta = UltraStarSongParser.ParseFile(songFilePath, out List<SongIssue> songIssues, Encoding.UTF8, false);
        if (songMeta == null)
        {
            Assert.Fail($"Failed to load song from path '{songFilePath}'");
        }

        if (!songIssues.IsNullOrEmpty())
        {
            string songIssuesCsv = songIssues.Select(songIssue => songIssue.Message).JoinWith("\n    - ");
            Assert.Fail($"Issues found with song at path '{songFilePath}':\n    - {songIssuesCsv}");
        }

        return songMeta;
    }
}
