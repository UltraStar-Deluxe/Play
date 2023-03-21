public class BeatPitchEventDto : JsonSerializable
{
    public int Beat { get; set; }
    public int MidiNote { get; set; }
    public float Frequency { get; set; }

    public BeatPitchEventDto(int midiNote, int beat, float frequency)
    {
        Beat = beat;
        MidiNote = midiNote;
        Frequency = frequency;
    }
}
