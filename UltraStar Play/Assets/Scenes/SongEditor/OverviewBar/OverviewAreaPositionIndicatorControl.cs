using UniInject;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class OverviewAreaPositionIndicatorControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(UxmlName = R.UxmlNames.overviewAreaPositionIndicator)]
    private VisualElement overviewAreaPositionIndicator;

    public void OnInjectionFinished()
    {
        songAudioPlayer.PositionEventStream.Subscribe(SetPositionInMillis);
    }

    private void SetPositionInMillis(double positionInMillis)
    {
        double positionInPercent = positionInMillis / songAudioPlayer.DurationInMillis;
        UpdatePosition(positionInPercent);
    }

    private void UpdatePosition(double positionInPercent)
    {
        float xPercent = (float)positionInPercent;
        overviewAreaPositionIndicator.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
    }
}
