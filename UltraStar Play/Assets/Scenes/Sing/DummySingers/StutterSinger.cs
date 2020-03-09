using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StutterSinger : AbstractDummySinger
{
    public override void UpdateSinging(double currentBeat)
    {
        Note noteAtCurrentBeat = GetNoteAtCurrentBeat(currentBeat);
        PitchEvent pitchEvent = null;
        if (noteAtCurrentBeat != null && Random.Range(0, 5) != 0)
        {
            pitchEvent = new PitchEvent(noteAtCurrentBeat.MidiNote + Random.Range(-3, 3));
        }
        playerController.PlayerNoteRecorder.HandlePitchEvent(pitchEvent);
    }
}
