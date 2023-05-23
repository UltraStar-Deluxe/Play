using UnityEngine;

public class ChangingOffsetSinger : AbstractDummySinger
{
    [Range(0, 11)]
    public int maxOffset = 5;
    
    [Range(0, 100)]
    public int randomness;
    
    private int noteOffset;
    private Note lastNote;
    

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        // Change noteOffset when note changes.
        Note noteAtBeat = GetNoteAtBeat(beat, true, false);
        if (lastNote != null
            && noteAtBeat != lastNote
            && Random.Range(0, 100) > 100 - randomness)
        {
            noteOffset += 1;
            if (noteOffset > maxOffset)
            {
                noteOffset = 0;
            }
        }
        lastNote = noteAtBeat;

        if (noteAtBeat == null)
        {
            return null;
        }

        int dummyMidiNote = noteAtBeat.MidiNote + noteOffset;
        int dummyMidiNoteInSingableNoteRange = (dummyMidiNote % 12) + (5 * 12);
        float frequency = 0;
        return new BeatPitchEvent(dummyMidiNoteInSingableNoteRange, beat, frequency);
    }
}
