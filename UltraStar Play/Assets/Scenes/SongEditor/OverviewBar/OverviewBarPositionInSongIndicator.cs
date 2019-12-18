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

    [Inject]
    private NoteArea noteArea;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private double widthInPercent;
    public double WidthInPercent
    {
        get
        {
            return widthInPercent;
        }
        set
        {
            widthInPercent = value;
            UpdatePositionAndWidth();
        }
    }

    private double positionInSongInPercent;
    public double PositionInSongInPercent
    {
        get
        {
            return positionInSongInPercent;
        }
        set
        {
            positionInSongInPercent = value;
            UpdatePositionAndWidth();
        }
    }

    void Start()
    {
        noteArea.ViewportEventStream.Subscribe(OnViewportChanged);
        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        WidthInPercent = viewportEvent.Width / songAudioPlayer.DurationOfSongInMillis;
    }

    private void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        PositionInSongInPercent = positionInSongInMillis / songAudioPlayer.DurationOfSongInMillis;
    }

    private void UpdatePositionAndWidth()
    {
        float xMin = (float)(PositionInSongInPercent - WidthInPercent);
        float xMax = (float)PositionInSongInPercent;

        rectTransform.anchorMin = new Vector2(xMin, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(xMax, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
