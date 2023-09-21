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
    protected static readonly double targetDurationInMillis = 4000;
    protected static readonly double maxDistanteToTargetDurationInMillis = 500;

    protected override string TestSceneName => "MediaFileFormatTestScene";

    private SongAudioPlayer songAudioPlayer;
    private SongAudioPlayer SongAudioPlayer
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

    protected IEnumerator AudioFileTest(string filePrefix)
    {
        return FileTest(filePrefix, audioFileFormatTestFolderPath);
    }

    protected IEnumerator VideoFileTest(string filePrefix)
    {
        return FileTest(filePrefix, videoFileFormatTestFolderPath);
    }

    private IEnumerator FileTest(string filePrefix, string folderPath)
    {
        LogAssert.ignoreFailingMessages = true;

        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();

        string songFilePath = GetSongMetaFilePath(filePrefix, folderPath);
        SongAudioPlayerCanLoadFileTest(songFilePath);

        yield return new WaitUntil(() => SongAudioPlayer.DurationOfSongInMillis > 0
                                         || TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, 1000));
    }

    private void SongAudioPlayerCanLoadFileTest(string songFilePath)
    {
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        SongMeta songMeta = LoadSongMeta(songFilePath);
        SongAudioPlayer.LoadAndPlaySongAudioAsObservable(songMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Assert.Fail($"SongAudioPlayer failed to load song {songFilePath}: {ex.Message}");
            })
            .Subscribe(evt =>
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

                    Debug.Log($"Successfully loaded song media {songMeta.Audio} after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms. Song duration: {songAudioPlayer.DurationOfSongInMillis} ms");
                });
    }

    protected string GetSongMetaFilePath(string filePrefix, string folderPath)
    {
        return $"{folderPath}/{filePrefix}TestSong.txt";
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
            string songIssuesCsv = songIssues.Select(songIssue => songIssue.Message).ToCsv("\n    - ", "", "");
            Assert.Fail($"Issues found with song at path '{songFilePath}':\n    - {songIssuesCsv}");
        }

        return songMeta;
    }
}
