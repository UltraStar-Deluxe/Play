using UniInject;
using UniRx;
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
        float viewportStartInMillis = viewportEvent.X;
        float viewportEndInMillis = viewportEvent.X + viewportEvent.Width;
        float startPercent = viewportStartInMillis / songAudioPlayer.DurationOfSongInMillis;
        float endPercent = viewportEndInMillis / songAudioPlayer.DurationOfSongInMillis;

        UpdatePositionAndWidth(startPercent, endPercent);
    }

    private void UpdatePositionAndWidth(float startPercent, float endPercent)
    {
        float xMinPercent = startPercent;
        float widthPercent = endPercent - startPercent;

        overviewAreaViewportIndicator.style.left = new StyleLength(new Length(xMinPercent * 100, LengthUnit.Percent));
        overviewAreaViewportIndicator.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
    }
}
