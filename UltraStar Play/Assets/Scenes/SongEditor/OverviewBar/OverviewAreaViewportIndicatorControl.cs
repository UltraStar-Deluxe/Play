using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UniRx;
using System;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaViewportIndicatorControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject(UxmlName = R.UxmlNames.overviewAreaViewportIndicator)]
    private VisualElement overviewAreaViewportIndicator;

    public void OnInjectionFinished()
    {
        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);
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
        float xMinPercent = (float)startPercent;
        float widthPercent = (float)(endPercent - startPercent);

        overviewAreaViewportIndicator.style.left = new StyleLength(new Length(xMinPercent * 100, LengthUnit.Percent));
        overviewAreaViewportIndicator.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
    }
}
