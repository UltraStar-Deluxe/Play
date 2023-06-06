using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerPitchIndicatorControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.noteContainer)]
    private VisualElement noteContainer;
    
    [Inject(UxmlName = R.UxmlNames.playerPitchIndicator)]
    private VisualElement playerPitchIndicator;
    
    [Inject(UxmlName = R.UxmlNames.pitchIndicatorIcon)]
    private VisualElement pitchIndicatorIcon;

    [Inject]
    private PlayerMicPitchTracker playerMicPitchTracker;

    [Inject]
    private AbstractSingSceneNoteDisplayer noteDisplayer;
    
    [Inject]
    private SongAudioPlayer songAudioPlayer;
    
    [Inject]
    private Settings settings;
    
    [Inject(Optional = true)]
    private MicProfile micProfile;
    
    private int lastMidiNote;

    private bool IsPitchIndicatorVisible => settings.ShowPitchIndicator 
                                            && noteDisplayer is not NoNoteSingSceneDisplayer
                                            && micProfile != null;
    
    public void OnInjectionFinished()
    {
        settings.ObserveEveryValueChanged(it => it.ShowPitchIndicator)
            .Subscribe(_ => playerPitchIndicator.SetVisibleByDisplay(IsPitchIndicatorVisible));

        if (micProfile != null)
        {
            pitchIndicatorIcon.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
        }
        playerMicPitchTracker.BeatAnalyzedEventStream.Subscribe(evt => OnBeatAnalyzedEvent(evt));
        UpdatePitchIndicatorPosition(MidiUtils.MidiNoteConcertPitch);
    }

    private void OnBeatAnalyzedEvent(BeatAnalyzedEvent beatAnalyzedEvent)
    {
        if (noteDisplayer == null
            || beatAnalyzedEvent == null
            || beatAnalyzedEvent.PitchEvent == null)
        {
            return;
        }

        lastMidiNote = beatAnalyzedEvent.RoundedRecordedMidiNote;
        UpdatePitchIndicatorPosition(lastMidiNote);
    }

    private void UpdatePitchIndicatorPosition(int midiNote)
    {
        if (!IsPitchIndicatorVisible)
        {
            return;
        }
        
        Vector2 yPosRangeFactor = noteDisplayer.GetYStartAndEndInPercentForMidiNote(midiNote);
        float height = 100f * (yPosRangeFactor.y - yPosRangeFactor.x);
        
        float yPosPercent = 100f * yPosRangeFactor.x;
        yPosPercent = NumberUtils.Limit(yPosPercent, 0, 100);
        float smoothYPos = playerPitchIndicator.style.top.value.value + (yPosPercent - playerPitchIndicator.style.top.value.value) * (10f * Time.deltaTime);

        int micDelay = 0;
        if (micProfile != null)
        {
            micDelay += micProfile.DelayInMillis;
            if (micProfile.IsInputFromConnectedClient)
            {
                micDelay += settings.ConnectedClientMessageBufferTimeInMillis;
            }
        }

        double positionInSongInMillisConsideringMicDelay = songAudioPlayer.PositionInSongInMillis - micDelay;
        float xPosPercent = 100f * noteDisplayer.GetXInPercent(positionInSongInMillisConsideringMicDelay);
        xPosPercent = NumberUtils.Limit(xPosPercent, 0, float.MaxValue);
        
        playerPitchIndicator.style.left =  new StyleLength(Length.Percent(xPosPercent));
        playerPitchIndicator.style.top =  new StyleLength(Length.Percent(smoothYPos));
        playerPitchIndicator.style.height = new StyleLength(Length.Percent(height));
    }

    public void Update()
    {
        if (!IsPitchIndicatorVisible)
        {
            return;
        }

        if (lastMidiNote > 0)
        {
            UpdatePitchIndicatorPosition(lastMidiNote);
        }
    }
}
