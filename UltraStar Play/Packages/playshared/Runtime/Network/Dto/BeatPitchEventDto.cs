public class BeatPitchEventDto : CompanionAppMessageDto
{
    public int Beat { get; set; }
    public int MidiNote { get; set; }
    public long UnixTimeMilliseconds { get; set; }

    public BeatPitchEventDto() : base(CompanionAppMessageType.BeatPitchEvent)
    {
    }

    public BeatPitchEventDto(int midiNote, int beat, long unixTimeMilliseconds) : this()
    {
        Beat = beat;
        MidiNote = midiNote;
        UnixTimeMilliseconds = unixTimeMilliseconds;
    }
}
