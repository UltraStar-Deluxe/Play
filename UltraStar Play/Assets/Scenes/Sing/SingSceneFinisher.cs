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

    private double positionInSongInMillisMax;

    private bool hasFinishedScene;

    private void Update()
    {
        double durationOfSongInMillis = songAudioPlayer.DurationOfSongInMillis;
        if (durationOfSongInMillis <= 0)
        {
            return;
        }

        Debug.Log("pos: " + songAudioPlayer.PositionInSongInMillis);
        
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
                && positionInSongInMillisMax > songAudioPlayer.PositionInSongInMillis)
            {
                // Do not go back to old time value.
                songAudioPlayer.PositionInSongInMillis = positionInSongInMillisMax - 1;
            }
        }
        else
        {
            double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;

            // Normal detection of song finished.
            // This only works when the position is not reset to zero when the AudioClip finishes.
            // 16 ms is roughly 1 frame at 60 FPS
            if (Math.Abs(durationOfSongInMillis - positionInSongInMillis) <= 16)
            {
                IsSongFinished = true;
                songVideoPlayer.FreezeVideo = true;
            }

            // Detect end of the song by looking for a falling flank in the playback position.
            if (hasBeenNearEndOfSong)
            {
                // The position is back to a previous value.
                if (positionInSongInMillis < positionInSongInMillisMax)
                {
                    IsSongFinished = true;
                    songVideoPlayer.FreezeVideo = true;
                    songAudioPlayer.StopAudio();
                }
                else
                {
                    positionInSongInMillisMax = positionInSongInMillis;
                }
            }
            else
            {
                // The position is near the end of the song.
                double missingMillis = durationOfSongInMillis - positionInSongInMillis;
                if (missingMillis < 500)
                {
                    hasBeenNearEndOfSong = true;
                }
            }

            // Detect end of the song by #END tag of txt file.
            // This can be used to skip the ending of the audio file.
            if (songMeta.End > 0
                // #END tag is in milliseconds (but #START is in seconds)
                && positionInSongInMillis > songMeta.End)
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
