using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UniRx;
using System;

#pragma warning disable CS0649

public class OverviewBarPositionInSongIndicator : MonoBehaviour, INeedInjection
{

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    void Start()
    {
        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);
    }

    private void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        double positionInSongInPercent = positionInSongInMillis / songAudioPlayer.DurationOfSongInMillis;
        UpdatePosition(positionInSongInPercent);
    }

    private void UpdatePosition(double positionInSongInPercent)
    {
        float x = (float)positionInSongInPercent;

        rectTransform.anchorMin = new Vector2(x, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(x, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
