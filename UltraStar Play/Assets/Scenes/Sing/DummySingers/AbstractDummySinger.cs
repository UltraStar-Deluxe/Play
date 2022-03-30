using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractDummySinger : MonoBehaviour, INeedInjection
{
    public int playerIndexToSimulate;

    private PlayerControl playerControl;

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

    protected abstract PitchEvent GetDummyPitchEvent(int beat, Note noteAtBeat);

    private void Update()
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

        Note noteAtBeat = GetNoteAtBeat(beatToAnalyze);
        if (noteAtBeat == null)
        {
            return;
        }

        PitchEvent pitchEvent = GetDummyPitchEvent(beatToAnalyze, noteAtBeat);
        playerControl.PlayerMicPitchTracker.FirePitchEvent(pitchEvent, beatToAnalyze, noteAtBeat, noteAtBeat.Sentence);
        playerControl.PlayerMicPitchTracker.GoToNextBeat();
    }

    private bool TryFindPlayerControl()
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

    protected double GetBeatDelayedByMicDelay(int beat)
    {
        double micDelayInBeats = (playerControl.MicProfile == null)
            ? 0
            : BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, playerControl.MicProfile.DelayInMillis);
        double delayedBeat = beat - micDelayInBeats;
        return delayedBeat;
    }

    protected Note GetNoteAtBeat(int beat)
    {
        Sentence recordingSentence = playerControl?.PlayerMicPitchTracker?.RecordingSentence;
        if (recordingSentence == null)
        {
            return null;
        }

        Note noteAtBeat = recordingSentence.Notes
            .Where(note => note.StartBeat <= beat && beat < note.EndBeat).FirstOrDefault();
        return noteAtBeat;
    }
}
