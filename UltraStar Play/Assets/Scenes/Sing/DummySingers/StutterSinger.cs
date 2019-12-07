using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StutterSinger : AbstractDummySinger
{
    public override void UpdateSinging(double currentBeat)
    {
        Sentence currentSentence = playerController?.CurrentSentence;
        if (currentSentence == null)
        {
            return;
        }

        int currentMidiNote = Random.Range(33, 70);
        Note noteAtCurrentBeat = PlayerNoteRecorder.GetNoteAtBeat(currentSentence, currentBeat);
        if (noteAtCurrentBeat != null)
        {
            currentMidiNote = noteAtCurrentBeat.MidiNote + Random.Range(-3, 3);
        }
        if (Random.Range(0, 5) == 0)
        {
            currentMidiNote = 0;
        }
        playerController.PlayerNoteRecorder.HandlePitchEvent(new PitchEvent(currentMidiNote));
    }
}
