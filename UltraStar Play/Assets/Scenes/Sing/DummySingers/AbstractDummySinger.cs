using UnityEngine;
using UniInject;
using System.Linq;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

abstract public class AbstractDummySinger : MonoBehaviour, INeedInjection
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

    protected abstract PitchEvent GetDummyPichtEvent(int beat, Note noteAtBeat);

    void Update()
    {
        int currentBeat = (int)singSceneControl.CurrentBeat;
        int beatToAnalyze = playerControl.PlayerPitchTracker.BeatToAnalyze;

        if (currentBeat > 0
            && beatToAnalyze <= currentBeat
            && playerControl.PlayerPitchTracker.RecordingSentence != null)
        {
            Note noteAtBeat = GetNoteAtBeat(beatToAnalyze);
            PitchEvent pitchEvent = GetDummyPichtEvent(beatToAnalyze, noteAtBeat);
            playerControl.PlayerPitchTracker.FirePitchEvent(pitchEvent, beatToAnalyze, noteAtBeat, noteAtBeat.Sentence);
            playerControl.PlayerPitchTracker.GoToNextBeat();
        }
    }

    public void SetPlayerController(PlayerControl playerControl)
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
        Sentence recordingSentence = playerControl?.PlayerPitchTracker?.RecordingSentence;
        if (recordingSentence == null)
        {
            return null;
        }

        Note noteAtBeat = recordingSentence.Notes
            .Where(note => note.StartBeat <= beat && beat < note.EndBeat).FirstOrDefault();
        return noteAtBeat;
    }
}
