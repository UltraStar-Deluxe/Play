using UnityEngine;
using UniInject;
using System.Linq;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

abstract public class AbstractDummySinger : MonoBehaviour, INeedInjection
{
    public int playerIndexToSimulate;

    protected PlayerController playerController;

    [Inject]
    protected SongMeta songMeta;

    private int lastBeat;

    void Awake()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }

    protected abstract PitchEvent GetDummyPichtEvent(int beat, Note noteAtBeat);

    public void UpdateSinging(double currentBeat)
    {
        if ((int)currentBeat <= (lastBeat + 1))
        {
            return;
        }

        int beat = (int)currentBeat;
        Note noteAtBeat = GetNoteAtBeat(beat);

        PitchEvent pitchEvent = GetDummyPichtEvent(beat, noteAtBeat);

        playerController.PlayerPitchTracker.FirePitchEvent(pitchEvent, beat, noteAtBeat);
        playerController.PlayerPitchTracker.GoToNextBeat();

        lastBeat = beat;
    }

    public void SetPlayerController(PlayerController playerController)
    {
        this.playerController = playerController;
        // Disable real microphone input for this player
        playerController.MicSampleRecorder.enabled = false;
    }

    protected double GetBeatDelayedByMicDelay(int beat)
    {
        double micDelayInBeats = (playerController.MicProfile == null)
            ? 0
            : BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, playerController.MicProfile.DelayInMillis);
        double delayedBeat = beat - micDelayInBeats;
        return delayedBeat;
    }

    protected Note GetNoteAtBeat(int beat)
    {
        Sentence recordingSentence = playerController?.PlayerPitchTracker?.RecordingSentence;
        if (recordingSentence == null)
        {
            return null;
        }

        Note noteAtBeat = recordingSentence.Notes
            .Where(note => note.StartBeat <= beat && beat < note.EndBeat).FirstOrDefault();
        return noteAtBeat;
    }
}
