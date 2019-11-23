using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerfectSinger : AbstractDummySinger
{
    protected override void UpdateSinging(PlayerController playerController, double currentBeat)
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
        playerController.PlayerNoteRecorder.OnPitchDetected(new PitchEvent(currentMidiNote));
    }
}
