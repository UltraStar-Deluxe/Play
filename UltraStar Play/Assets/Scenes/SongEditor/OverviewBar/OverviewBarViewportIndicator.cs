using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UniRx;
using System;

#pragma warning disable CS0649

public class OverviewBarViewportIndicator : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private NoteArea noteArea;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    void Start()
    {
        noteArea.ViewportEventStream.Subscribe(OnViewportChanged);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        double viewportStartInMillis = viewportEvent.X;
        double viewportEndInMillis = viewportEvent.X + viewportEvent.Width;
        double startPercent = viewportStartInMillis / songAudioPlayer.DurationOfSongInMillis;
        double endPercent = viewportEndInMillis / songAudioPlayer.DurationOfSongInMillis;

        UpdatePositionAndWidth(startPercent, endPercent);
    }

    private void UpdatePositionAndWidth(double startPercent, double endPercent)
    {
        float xMin = (float)startPercent;
        float xMax = (float)endPercent;

        rectTransform.anchorMin = new Vector2(xMin, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(xMax, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
