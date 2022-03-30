﻿using UniInject;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaPositionInSongIndicatorControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(UxmlName = R.UxmlNames.overviewAreaPositionInSongIndicator)]
    private VisualElement overviewAreaPositionInSongIndicator;

    public void OnInjectionFinished()
    {
        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);
    }

    private void SetPositionInSongInMillis(float positionInSongInMillis)
    {
        float positionInSongInPercent = positionInSongInMillis / songAudioPlayer.DurationOfSongInMillis;
        UpdatePosition(positionInSongInPercent);
    }

    private void UpdatePosition(float positionInSongInPercent)
    {
        float xPercent = (float)positionInSongInPercent;
        overviewAreaPositionInSongIndicator.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
    }
}
