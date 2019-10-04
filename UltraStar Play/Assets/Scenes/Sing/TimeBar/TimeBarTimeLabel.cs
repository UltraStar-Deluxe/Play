using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TimeBarTimeLabel : MonoBehaviour
{
    public bool showRemainingTime = true;

    private Text text;
    private SingSceneController singSceneController;

    void OnEnable()
    {
        text = GetComponent<Text>();
        singSceneController = SingSceneController.Instance;
        InvokeRepeating("UpdateText", 0f, 1f);
    }

    void UpdateText()
    {
        SetText(singSceneController.PositionInSongInMillis, singSceneController.DurationOfSongInMillis);
    }

    private void SetText(double positionInSongInMillis, double durationOfSongInMillis)
    {
        if (showRemainingTime)
        {
            double remainingTimeInSeconds = (durationOfSongInMillis - positionInSongInMillis) / 1000;
            SetText(remainingTimeInSeconds);
        }
        else
        {
            double positionInSongInSeconds = positionInSongInMillis / 1000;
            SetText(positionInSongInSeconds);
        }
    }

    private void SetText(double timeInSeconds)
    {
        int mins = (int)Math.Floor(timeInSeconds / 60);
        string minsPadding = (mins < 10) ? "0" : "";
        int secs = (int)Math.Floor(timeInSeconds % 60);
        string secsPadding = (secs < 10) ? "0" : "";
        text.text = minsPadding + mins + ":" + secsPadding + secs;
    }
}
