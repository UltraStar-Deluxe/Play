using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerfectSinger : AbstractDummySinger
{
    public int offset = 12;

    public override void UpdateSinging(double currentBeat)
    {
        Sentence currentSentence = playerController?.CurrentSentence;
        if (currentSentence == null)
        {
            return;
        }

        int currentMidiNote = 0;
        Note noteAtCurrentBeat = PlayerNoteRecorder.GetNoteAtBeat(currentSentence, currentBeat);
        if (noteAtCurrentBeat != null)
        {
            currentMidiNote = noteAtCurrentBeat.MidiNote + offset;
        }
        playerController.PlayerNoteRecorder.HandlePitchEvent(new PitchEvent(currentMidiNote));
    }
}
