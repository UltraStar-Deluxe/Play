public class BeatPitchEvent : PitchEvent
{
    public int Beat { get; set; }

    public BeatPitchEvent(int midiNote, int beat, float frequency)
        : base(midiNote, frequency)
    {
        Beat = beat;
    }
}
