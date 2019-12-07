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
        Sentence currentSentence = playerController?.CurrentSentence;
        if (currentSentence == null)
        {
            return;
        }

        int currentMidiNote = 0;
        Note noteAtCurrentBeat = PlayerNoteRecorder.GetNoteAtBeat(currentSentence, currentBeat);
        if (noteAtCurrentBeat != null)
        {
            currentMidiNote = noteAtCurrentBeat.MidiNote + noteOffset;
        }
        else if (lastNote != null)
        {
            // Change noteOffset on falling flank of note
            // (falling flank: now there is no note, but in last frame there was one)
            noteOffset = (noteOffset + 1) % 5;
        }
        playerController.PlayerNoteRecorder.HandlePitchEvent(new PitchEvent(currentMidiNote));

        lastNote = noteAtCurrentBeat;
    }
}
