public class ChangingOffsetSinger : AbstractDummySinger
{
    public int maxOffset = 5;
    private int noteOffset;
    Note lastNote;

    protected override PitchEvent GetDummyPichtEvent(int beat, Note noteAtBeat)
    {
        PitchEvent pitchEvent = null;
        if (noteAtBeat != null)
        {
            pitchEvent = new PitchEvent(noteAtBeat.MidiNote + noteOffset);
        }

        // Change noteOffset when note changes.
        if (lastNote != null && noteAtBeat != lastNote)
        {
            noteOffset = (noteOffset + 1) % maxOffset;
        }
        lastNote = noteAtBeat;
        return pitchEvent;
    }
}
