using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

public abstract class AbstractMediaFileFormatTests : AbstractPlayModeTest
{
    protected static readonly string audioFileFormatTestFolderPath = Application.dataPath + "/PlayModeTests/MediaFileFormatTests/AudioFileFormatTests";
    protected static readonly string videoFileFormatTestFolderPath = Application.dataPath + "/PlayModeTests/MediaFileFormatTests/VideoFileFormatTests";
    protected static readonly string webViewFileFormatTestFolderPath = Application.dataPath + "/PlayModeTests/MediaFileFormatTests/WebViewTests";

    protected static readonly double localFileTargetDurationInMillis = 4000;
    protected static readonly double webViewTargetDurationInMillis = 242561;
    private static readonly double maxDistanteToTargetDurationInMillis = 500;

    protected static readonly long localFileMaxWaitTimeInMillis = 5000;
    protected static readonly long webViewMaxWaitTimeInMillis = 30000;

    protected override string TestSceneName => "MediaFileFormatTestScene";

    private SongAudioPlayer songAudioPlayer;
    protected SongAudioPlayer SongAudioPlayer
    {
        get
        {
            if (songAudioPlayer == null)
            {
                songAudioPlayer = GameObject.FindObjectOfType<SongAudioPlayer>();
                if (songAudioPlayer == null)
                {
                    Assert.Fail("Failed to find SongAudioPlayer in scene.");
                }
            }

            return songAudioPlayer;
        }
    }

    protected IEnumerator ShouldLoadAudioFile(string txtFileName)
    {
        yield return ShouldLoadFile(txtFileName, audioFileFormatTestFolderPath, localFileTargetDurationInMillis, localFileMaxWaitTimeInMillis);
    }

    protected IEnumerator ShouldLoadVideoFile(string txtFileName)
    {
        yield return ShouldLoadFile(txtFileName, videoFileFormatTestFolderPath, localFileTargetDurationInMillis, localFileMaxWaitTimeInMillis);
    }

    protected IEnumerator WebViewFileTest(string filePrefix, double targetDurationInMillis)
    {
        yield return ShouldLoadFile(filePrefix, webViewFileFormatTestFolderPath, targetDurationInMillis, webViewMaxWaitTimeInMillis);
    }

    private IEnumerator ShouldLoadFile(string txtFileName, string folderPath, double targetDurationInMillis, long maxWaitTimeInMillis)
    {
        LogAssert.ignoreFailingMessages = true;

        string songFilePath = GetSongMetaFilePath(txtFileName, folderPath);
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        bool hasFailed = false;

        SongMeta songMeta = LoadSongMeta(songFilePath);
        SongAudioPlayer.LoadAndPlaySongAudioAsObservable(songMeta)
            .Select(evt =>
            {
                double durationOfSongInMillis = SongAudioPlayer.DurationOfSongInMillis;
                if (durationOfSongInMillis <= 0)
                {
                    Assert.Fail("SongAudioPlayer failed to load song (duration is 0).");
                }

                if (Math.Abs(durationOfSongInMillis - targetDurationInMillis) > maxDistanteToTargetDurationInMillis)
                {
                    Assert.Fail($"SongAudioPlayer loaded song with wrong duration {durationOfSongInMillis} ms (should be near {targetDurationInMillis} ms).");
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
                Debug.Log($"Successfully loaded song media (audio: '{songMeta.Audio}', audioUrl: '{songMeta.AudioUrl}', videoUrl: '{songMeta.VideoUrl}') after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms. Song duration: {songAudioPlayer.DurationOfSongInMillis} ms");
            });

        yield return new WaitUntil(() => SongAudioPlayer.DurationOfSongInMillis > 0
                                         || hasFailed
                                         || TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis));

        if (hasFailed)
        {
            Assert.Fail("Test failed. Check log for details.");
        }
        else if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis))
        {
            Assert.Fail($"Failed to load song media (audio: '{songMeta.Audio}', audioUrl: '{songMeta.AudioUrl}', videoUrl: '{songMeta.VideoUrl}') after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms.");
        }
    }

    protected string GetSongMetaFilePath(string fileName, string folderPath)
    {
        return $"{folderPath}/{fileName}";
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
