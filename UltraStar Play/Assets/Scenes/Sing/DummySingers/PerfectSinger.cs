using UniInject;

public class PerfectSinger : AbstractDummySinger
{
    public int offset = 12;

    protected override PitchEvent GetDummyPichtEvent(int beat, Note noteAtBeat)
    {
        PitchEvent pitchEvent = null;
        if (noteAtBeat != null)
        {
            pitchEvent = new PitchEvent(noteAtBeat.MidiNote + offset);
        }
        return pitchEvent;
    }
}
