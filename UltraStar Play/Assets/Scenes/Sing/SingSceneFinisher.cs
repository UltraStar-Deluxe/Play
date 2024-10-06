using System;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Handles automatically finishing the SingScene.
// The Unity AudioPlayer changes the playback position back to zero when the AudioClip has finished.
// This script detects this falling flank in the playback position
// and will finish the scene with a small delay afterwards.
// To prevent premature ending the scene, it is only watched for the falling flank in the playback position
// when the song has been near its end already.
public class SingSceneFinisher : MonoBehaviour, INeedInjection
{
    private const int NearEndOfSongThresholdInMillis = 500;
    private const float WaitTimeAfterSongFinishedInSeconds = 1.5f;
    private const float WaitTimeAfterNearEndOfSongThresholdInSeconds = WaitTimeAfterSongFinishedInSeconds + (NearEndOfSongThresholdInMillis / 1000f);

    public bool IsSongFinished { get; private set; }

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongVideoPlayer songVideoPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SingSceneData sceneData;

    private double positionInMillisMax;

    private bool hasBeenNearEndOfSong;
    private float durationAfterHasBeenNearEndOfSongInSeconds;

    private float durationAfterSongFinishedInSeconds;
    private bool hasFinishedScene;
    private bool isEarlyFinish;

    private void Update()
    {
        double durationInMillis = songAudioPlayer.DurationInMillis;
        if (durationInMillis <= 0)
        {
            return;
        }

        double positionInMillis = songAudioPlayer.PositionInMillis;
        positionInMillisMax = Math.Max(positionInMillisMax, positionInMillis);

        if (IsSongFinished)
        {
            UpdateAfterSongFinished(positionInMillis);
        }
        else
        {
            DetectSongFinished(positionInMillis, durationInMillis);
        }
    }

    private void DetectSongFinished(double positionInMillis, double durationInMillis)
    {
        // Normal detection of song finished.
        // This only works when the position is not reset to zero when the AudioClip finishes.
        // 16 ms is roughly 1 frame at 60 FPS
        if (Math.Abs(durationInMillis - positionInMillis) <= 16
            || positionInMillis >= durationInMillis)
        {
            SetSongFinished();
            return;
        }

        double remainingMillis = durationInMillis - positionInMillis;
        if (remainingMillis < NearEndOfSongThresholdInMillis)
        {
            hasBeenNearEndOfSong = true;
        }

        if (hasBeenNearEndOfSong)
        {
            // Detect end of the song by looking for a falling flank in the playback position.
            // Some API (e.g. Unity AudioSource) set the time to 0 after the song has finished.
            if (positionInMillis < positionInMillisMax)
            {
                SetSongFinished();
                return;
            }

            // Detect end of the song by being near the end of the song for too long.
            // This is a workaround for some API (e.g. libVLC) is not reaching the exact duration of the song in its playback position.
            durationAfterHasBeenNearEndOfSongInSeconds += Time.deltaTime;
            if (durationAfterHasBeenNearEndOfSongInSeconds >= WaitTimeAfterNearEndOfSongThresholdInSeconds)
            {
                SetSongFinished();
                // Finish scene immediately, because the song has been near the end for too long.
                FinishScene();
                return;
            }
        }

        // Detect end of the song by #END tag of txt file.
        // This can be used to skip the ending of the audio file.
        if (songMeta.EndInMillis > 0
            && positionInMillis > songMeta.EndInMillis)
        {
            SetSongFinished();
            return;
        }
    }

    private void SetSongFinished()
    {
        IsSongFinished = true;
        songVideoPlayer.FreezeVideo = true;
        songAudioPlayer.PositionInMillis = positionInMillisMax;
        songAudioPlayer.PauseAudio();
    }

    private void UpdateAfterSongFinished(double positionInMillis)
    {
        durationAfterSongFinishedInSeconds += Time.deltaTime;
        if (durationAfterSongFinishedInSeconds >= WaitTimeAfterSongFinishedInSeconds
            && !hasFinishedScene)
        {
            FinishScene();
        }

        if (hasBeenNearEndOfSong
            && positionInMillisMax > positionInMillis)
        {
            // Do not go back to old time value after the song has finished.
            // Some API (e.g. libVLC) set the time to 0 after the song has finished.
            songAudioPlayer.PositionInMillis = positionInMillisMax - 1;
        }
    }

    private void FinishScene()
    {
        hasFinishedScene = true;
        singSceneControl.FinishScene(!isEarlyFinish, true);
    }

    public void TriggerEarlySongFinish()
    {
        if (IsSongFinished)
        {
            return;
        }

        Debug.Log("Trigger early song finish");
        IsSongFinished = true;
        isEarlyFinish = true;
    }
}
