using System;
using UnityEngine;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

abstract public class AbstractDummySinger : MonoBehaviour, INeedInjection
{
    public int playerIndexToSimulate;

    protected PlayerController playerController;

    [Inject]
    protected SongMeta songMeta;

    void Awake()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }

    public abstract void UpdateSinging(double currentBeat);

    public void SetPlayerController(PlayerController playerController)
    {
        this.playerController = playerController;
        // Disable real microphone input for this player
        playerController.PlayerNoteRecorder.SetMicrophonePitchTrackerEnabled(false);
    }

    protected Note GetNoteAtCurrentBeat(double currentBeat)
    {
        Sentence currentSentence = playerController?.GetRecordingSentence();
        if (currentSentence == null)
        {
            return null;
        }

        double micDelayInBeats = (playerController.MicProfile == null) ? 0 : BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, playerController.MicProfile.DelayInMillis);
        Note noteAtCurrentBeat = PlayerNoteRecorder.GetNoteAtBeat(currentSentence, currentBeat - micDelayInBeats);
        return noteAtCurrentBeat;
    }
}