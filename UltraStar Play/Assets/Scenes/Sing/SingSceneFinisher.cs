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
    public bool IsSongFinished { get; private set; }

    private bool hasBeenNearEndOfSong;
    private bool isEarlyFinish;
    private float durationAfterSongFinishedInSeconds;

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

    private bool hasFinishedScene;

    private void Update()
    {
        double durationInMillis = songAudioPlayer.DurationInMillis;
        if (durationInMillis <= 0)
        {
            return;
        }

        if (IsSongFinished)
        {
            durationAfterSongFinishedInSeconds += Time.deltaTime;
            if (durationAfterSongFinishedInSeconds >= 1.5f
                && !hasFinishedScene)
            {
                hasFinishedScene = true;
                singSceneControl.FinishScene(!isEarlyFinish, true);
            }

            if (hasBeenNearEndOfSong
                && positionInMillisMax > songAudioPlayer.PositionInMillis)
            {
                // Do not go back to old time value.
                songAudioPlayer.PositionInMillis = positionInMillisMax - 1;
            }
        }
        else
        {
            double positionInMillis = songAudioPlayer.PositionInMillis;

            // Normal detection of song finished.
            // This only works when the position is not reset to zero when the AudioClip finishes.
            // 16 ms is roughly 1 frame at 60 FPS
            if (Math.Abs(durationInMillis - positionInMillis) <= 16)
            {
                IsSongFinished = true;
                songVideoPlayer.FreezeVideo = true;
            }

            // Detect end of the song by looking for a falling flank in the playback position.
            if (hasBeenNearEndOfSong)
            {
                // The position is back to a previous value.
                if (positionInMillis < positionInMillisMax)
                {
                    IsSongFinished = true;
                    songVideoPlayer.FreezeVideo = true;
                    songAudioPlayer.PauseAudio();
                }
                else
                {
                    positionInMillisMax = positionInMillis;
                }
            }
            else
            {
                // The position is near the end of the song.
                double missingMillis = durationInMillis - positionInMillis;
                if (missingMillis < 500)
                {
                    hasBeenNearEndOfSong = true;
                }
            }

            // Detect end of the song by #END tag of txt file.
            // This can be used to skip the ending of the audio file.
            if (songMeta.EndInMillis > 0
                // #END tag is in milliseconds (but #START is in seconds)
                && positionInMillis > songMeta.EndInMillis)
            {
                IsSongFinished = true;
            }
        }
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
