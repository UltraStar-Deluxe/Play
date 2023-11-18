using CommonOnlineMultiplayer;

public class BeatAnalyzedEventNetcodeRequestDto : NetcodeRequestDto
{
    public PitchEvent PitchEvent { get; private set; }
    public int Beat { get; private set; }
    public int RoundedRecordedMidiNote { get; private set; }
    public int RecordedMidiNote { get; private set; }

    public BeatAnalyzedEventNetcodeRequestDto()
        : base(ENetcodeMessageType.BeatAnalyzedEventRequest)
    {
    }

    public BeatAnalyzedEventNetcodeRequestDto(
        PitchEvent pitchEvent,
        int beat,
        int roundedRecordedMidiNote,
        int recordedMidiNote)
        : this()
    {
        PitchEvent = pitchEvent;
        Beat = beat;
        RoundedRecordedMidiNote = roundedRecordedMidiNote;
        RecordedMidiNote = recordedMidiNote;
    }
}
