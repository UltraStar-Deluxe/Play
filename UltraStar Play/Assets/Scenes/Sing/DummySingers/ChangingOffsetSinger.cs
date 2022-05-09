
public class ChangingOffsetSinger : AbstractDummySinger
{
    public int maxOffset = 5;
    private int noteOffset;
    private Note lastNote;

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        // Change noteOffset when note changes.
        Note noteAtBeat = GetNoteAtBeat(beat);
        if (lastNote != null && noteAtBeat != lastNote)
        {
            noteOffset = noteOffset + 1;
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
        return new BeatPitchEvent(dummyMidiNoteInSingableNoteRange, beat);
    }
}
