
public class PerfectSinger : AbstractDummySinger
{
    public int offset = 12;

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        Note noteAtBeat = GetNoteAtBeat(beat);
        if (noteAtBeat != null)
        {
            return new BeatPitchEvent(noteAtBeat.MidiNote + offset, beat);
        }

        return null;
    }
}
