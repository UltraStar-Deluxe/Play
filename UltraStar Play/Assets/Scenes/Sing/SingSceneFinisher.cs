using System;
using UnityEngine;

// Handles automatically finishing the SingScene.
// The Unity AudioPlayer changes the playback position back to zero when the AudioClip has finished.
// This script detects this falling flank in the playback position
// and will finish the scene with a small delay afterwards.
// To prevent premature ending the scene, it is only watched for the falling flank in the playback position
// when the song has been near its end already.
public class SingSceneFinisher : MonoBehaviour
{
    private bool hasBeenNearEndOfSong;
    private bool isSongFinished;
    private float durationAfterSongFinishedInSeconds;

    private SingSceneController singSceneController;

    private double positionInSongInMillisOld;

    void Awake()
    {
        singSceneController = FindObjectOfType<SingSceneController>();
    }

    void Update()
    {
        double durationOfSongInMillis = singSceneController.DurationOfSongInMillis;
        if (durationOfSongInMillis <= 0)
        {
            return;
        }

        if (isSongFinished)
        {
            durationAfterSongFinishedInSeconds += Time.deltaTime;
            if (durationAfterSongFinishedInSeconds >= 1)
            {
                singSceneController.FinishScene();
            }
        }
        else
        {
            double positionInSongInMillis = singSceneController.PositionInSongInMillis;

            // Normal detection of song finished.
            // This only works when the position is not reset to zero when the AudioClip finishes.
            if (Math.Abs(durationOfSongInMillis - positionInSongInMillis) <= 1)
            {
                isSongFinished = true;
            }

            // Detection of the end of the song by looking for a falling flank in the position in the song.
            if (hasBeenNearEndOfSong)
            {
                // The position is back to the start.
                if (positionInSongInMillis < 1000 && positionInSongInMillis < positionInSongInMillisOld)
                {
                    isSongFinished = true;
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
        }
    }
}