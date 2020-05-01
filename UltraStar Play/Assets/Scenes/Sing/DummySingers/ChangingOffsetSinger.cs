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
        else if (lastNote != null && noteAtBeat != lastNote)
        {
            // Change noteOffset when note changes.
            noteOffset = (noteOffset + 1) % maxOffset;
        }
        lastNote = noteAtBeat;
        return pitchEvent;
    }
}
