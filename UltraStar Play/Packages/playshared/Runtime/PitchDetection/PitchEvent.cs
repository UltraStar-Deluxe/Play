public class PitchEvent
{
    public int MidiNote { get; set; }
    public float Frequency { get; set; }

    public PitchEvent(int midiNote, float frequency)
    {
        MidiNote = midiNote;
        Frequency = frequency;
    }

    public override string ToString()
    {
        return $"PitchEvent(MidiNote: {MidiNote}, Frequency: {Frequency})";
    }
}
