public class BeatPitchEvent : PitchEvent
{
    public int Beat { get; set; }

    public BeatPitchEvent(int midiNote, int beat)
        : base(midiNote)
    {
        Beat = beat;
    }
}
