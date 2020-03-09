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
        Note noteAtCurrentBeat = GetNoteAtCurrentBeat(currentBeat);
        PitchEvent pitchEvent = null;
        if (noteAtCurrentBeat != null)
        {
            pitchEvent = new PitchEvent(noteAtCurrentBeat.MidiNote + offset);
        }
        playerController.PlayerNoteRecorder.HandlePitchEvent(pitchEvent);
    }
}
