
public class PerfectSinger : AbstractDummySinger
{
    public int offset = 12;

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        Note noteAtBeat = GetNoteAtBeat(beat, true, false);
        if (noteAtBeat != null)
        {
            float frequency = 0;
            return new BeatPitchEvent(noteAtBeat.MidiNote + offset, beat, frequency);
        }

        return null;
    }
}
