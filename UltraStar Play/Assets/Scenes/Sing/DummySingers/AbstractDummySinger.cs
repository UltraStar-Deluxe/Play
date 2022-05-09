using System.Collections.Generic;
using System.Linq;
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

        int currentBeat = (int)singSceneControl.CurrentBeat;
        int beatToAnalyze = playerControl.PlayerMicPitchTracker.BeatToAnalyze;
        if (currentBeat <= 0
            || beatToAnalyze > currentBeat
            || playerControl.PlayerMicPitchTracker.RecordingSentence == null)
        {
            return;
        }

        BeatPitchEvent pitchEvent = GetDummyPitchEvent(beatToAnalyze);
        FirePitchEvent(pitchEvent, beatToAnalyze);
    }

    protected void FirePitchEvent(BeatPitchEvent pitchEvent, int fallbackBeat)
    {
        int beat = pitchEvent != null
            ? pitchEvent.Beat
            : fallbackBeat;
        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.Voice, beat);
        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(sentenceAtBeat, beat);
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

    public void SetPlayerControl(PlayerControl playerControl)
    {
        this.playerControl = playerControl;
        // Disable real microphone input for this player
        playerControl.MicSampleRecorder.enabled = false;
    }

    protected Sentence GetSentenceAtBeat(int beat)
    {
        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.Voice, beat);
        return sentenceAtBeat;
    }

    protected Note GetNoteAtBeat(int beat)
    {
        Sentence sentenceAtBeat = GetSentenceAtBeat(beat);
        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(sentenceAtBeat, beat);
        return noteAtBeat;
    }
}
