using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangingOffsetSinger : AbstractDummySinger
{
    public int maxOffset = 5;
    private int noteOffset;
    Note lastNote;

    public override void UpdateSinging(double currentBeat)
    {
        Note noteAtCurrentBeat = GetNoteAtCurrentBeat(currentBeat);
        PitchEvent pitchEvent = null;
        if (noteAtCurrentBeat != null)
        {
            pitchEvent = new PitchEvent(noteAtCurrentBeat.MidiNote + noteOffset);
        }
        else if (lastNote != null)
        {
            // Change noteOffset on falling flank of note
            // (falling flank: now there is no note, but in last frame there was one)
            noteOffset = (noteOffset + 1) % 5;
        }
        playerController.PlayerNoteRecorder.HandlePitchEvent(pitchEvent, currentBeat, true);

        lastNote = noteAtCurrentBeat;
    }
}
