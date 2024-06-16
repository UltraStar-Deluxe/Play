using System.Collections.Generic;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractDummySinger : MonoBehaviour, INeedInjection
{
    public int playerIndexToSimulate;

    protected PlayerControl playerControl;

    [Inject]
    protected SingSceneControl singSceneControl;

    [Inject]
    protected SongMeta songMeta;
    
    [Inject]
    protected MicSampleRecorderManager micSampleRecorderManager;

    private readonly HashSet<int> analyzedBeats = new();

    protected virtual int BeatToAnalyze => playerControl.PlayerMicPitchTracker.BeatToAnalyze;
    
    void Awake()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }

    protected abstract BeatPitchEvent GetDummyPitchEvent(int beat);

    protected virtual void Update()
    {
        if (playerControl == null)
        {
            if (!TryFindPlayerControl())
            {
                return;
            }
        }
        
        // Disable mic input
        if (playerControl.PlayerMicPitchTracker != null
            && playerControl.PlayerMicPitchTracker.IsRecording.Value)
        {
            playerControl.PlayerMicPitchTracker.StopRecording();
        }

        int currentBeat = (int)singSceneControl.CurrentBeat;
        int beatToAnalyze = BeatToAnalyze;
        if (currentBeat <= 0
            || beatToAnalyze > currentBeat
            || playerControl.PlayerMicPitchTracker.RecordingSentence == null
            || analyzedBeats.Contains(beatToAnalyze))
        {
            return;
        }
        
        BeatPitchEvent pitchEvent = GetDummyPitchEvent(beatToAnalyze);
        FirePitchEvent(pitchEvent, beatToAnalyze);

        if (pitchEvent != null)
        {
            analyzedBeats.Add(pitchEvent.Beat);
        }
    }

    protected void FirePitchEvent(BeatPitchEvent pitchEvent, int fallbackBeat)
    {
        int beat = pitchEvent != null
            ? pitchEvent.Beat
            : fallbackBeat;
        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.Voice, beat, true, false);
        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(sentenceAtBeat, beat, true, false);
        playerControl.PlayerMicPitchTracker.FirePitchEvent(pitchEvent, beat, noteAtBeat, sentenceAtBeat);
    }

    protected bool TryFindPlayerControl()
    {
        if (singSceneControl.PlayerControls.IsNullOrEmpty()
            || playerIndexToSimulate >= singSceneControl.PlayerControls.Count)
        {
            return false;
        }

        playerControl = singSceneControl.PlayerControls[playerIndexToSimulate];
        return true;
    }

    public void SetPlayerControl(PlayerControl newPlayerControl)
    {
        this.playerControl = newPlayerControl;
        
        // Disable real microphone input for this player
        if (newPlayerControl == null
            || newPlayerControl.MicProfile == null)
        {
            return;
        }
        
        MicSampleRecorder micSampleRecorder = micSampleRecorderManager.GetOrCreateMicSampleRecorder(this.playerControl.MicProfile);
        if (micSampleRecorder != null)
        {
            micSampleRecorder.StopRecording();
            micSampleRecorder.enabled = false;
        }
    }

    protected Sentence GetSentenceAtBeat(int beat)
    {
        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.Voice, beat, true, false);
        return sentenceAtBeat;
    }

    protected Note GetNoteAtBeat(int beat, bool inclusiveStartBeat = true, bool inclusiveEndBeat = true)
    {
        Sentence sentenceAtBeat = GetSentenceAtBeat(beat);
        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(sentenceAtBeat, beat, inclusiveStartBeat, inclusiveEndBeat);
        return noteAtBeat;
    }
}
