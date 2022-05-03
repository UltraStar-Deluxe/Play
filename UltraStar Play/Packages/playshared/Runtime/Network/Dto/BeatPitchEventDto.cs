public class BeatPitchEventDto
{
    public int Beat { get; set; }
    public int MidiNote { get; set; }

    public BeatPitchEventDto(int midiNote, int beat)
    {
        Beat = beat;
        MidiNote = midiNote;
    }
}
