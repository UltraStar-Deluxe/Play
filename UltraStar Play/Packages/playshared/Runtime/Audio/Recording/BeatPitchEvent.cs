public class BeatPitchEvent : PitchEvent
{
    public int Beat { get; set; }

    public BeatPitchEvent(int midiNote, int beat, float frequency)
        : base(midiNote, frequency)
    {
        Beat = beat;
    }
    
    public override string ToString()
    {
        return $"BeatPitchEvent(Beat: {Beat}, MidiNote: {MidiNote}, Frequency: {Frequency})";
    }
}
