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
    protected SingSceneController singSceneController;

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
        int currentBeat = (int)singSceneController.CurrentBeat;
        int beatToAnalyze = playerController.PlayerPitchTracker.BeatToAnalyze;

        if (currentBeat > 0
            && beatToAnalyze <= currentBeat
            && playerController.PlayerPitchTracker.RecordingSentence != null)
        {
            Note noteAtBeat = GetNoteAtBeat(beatToAnalyze);
            PitchEvent pitchEvent = GetDummyPichtEvent(beatToAnalyze, noteAtBeat);
            playerController.PlayerPitchTracker.FirePitchEvent(pitchEvent, beatToAnalyze, noteAtBeat, noteAtBeat.Sentence);
            playerController.PlayerPitchTracker.GoToNextBeat();
        }
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
