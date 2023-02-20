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
    private SongMeta songMeta;

    [Inject]
    private SingSceneData sceneData;

    private double positionInSongInMillisOld;

    private bool hasFinishedScene;

    private void Update()
    {
        double durationOfSongInMillis = singSceneControl.DurationOfSongInMillis;
        if (durationOfSongInMillis <= 0)
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
        }
        else
        {
            double positionInSongInMillis = singSceneControl.PositionInSongInMillis;

            // Normal detection of song finished.
            // This only works when the position is not reset to zero when the AudioClip finishes.
            if (Math.Abs(durationOfSongInMillis - positionInSongInMillis) <= 1)
            {
                IsSongFinished = true;
            }

            // Detect end of the song by looking for a falling flank in the playback position.
            if (hasBeenNearEndOfSong)
            {
                // The position is back to the start.
                if (positionInSongInMillis < 1000 && positionInSongInMillis < positionInSongInMillisOld)
                {
                    IsSongFinished = true;
                }
                positionInSongInMillisOld = positionInSongInMillis;
            }
            else
            {
                // The position is near the end of the song.
                double missingMillis = durationOfSongInMillis - positionInSongInMillis;
                if (missingMillis < 1000)
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
