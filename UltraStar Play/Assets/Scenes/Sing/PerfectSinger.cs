using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerfectSinger : MonoBehaviour
{

    private SingSceneController singSceneController;

    void Start()
    {
        singSceneController = SingSceneController.Instance;

        // Disable other pitch trackers
        foreach (MicrophonePitchTracker microphonePitchTracker in FindObjectsOfType<MicrophonePitchTracker>())
        {
            microphonePitchTracker.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        double currentBeat = singSceneController.CurrentBeat;
        foreach (PlayerController playerController in singSceneController.PlayerControllers)
        {
            UpdatePerfectSinging(playerController, currentBeat);
        }
    }

    private void UpdatePerfectSinging(PlayerController playerController, double currentBeat)
    {
        Sentence currentSentence = playerController.CurrentSentence;
        if (currentSentence == null)
        {
            return;
        }

        int currentMidiNote = 0;
        Note noteAtCurrentBeat = PlayerNoteRecorder.GetNoteAtBeat(currentSentence, currentBeat);
        if (noteAtCurrentBeat != null)
        {
            currentMidiNote = noteAtCurrentBeat.MidiNote;
        }
        playerController.PlayerNoteRecorder.OnPitchDetected(currentMidiNote);
    }
}
