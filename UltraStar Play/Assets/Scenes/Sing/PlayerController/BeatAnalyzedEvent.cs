public class BeatAnalyzedEvent
{
    public PitchEvent PitchEvent { get; private set; }
    public int Beat { get; private set; }
    public Note NoteAtBeat { get; private set; }
    public Sentence SentenceAtBeat { get; private set; }
    public int RoundedRecordedMidiNote { get; private set; }
    public int RecordedMidiNote { get; private set; }

    public BeatAnalyzedEvent(PitchEvent pitchEvent,
        int beat,
        Note noteAtBeat,
        Sentence sentenceAtBeat,
        int recordedMidiNote,
        int roundedRecordedMidiNote)
    {
        PitchEvent = pitchEvent;
        Beat = beat;
        NoteAtBeat = noteAtBeat;
        SentenceAtBeat = sentenceAtBeat;
        RecordedMidiNote = recordedMidiNote;
        RoundedRecordedMidiNote = roundedRecordedMidiNote;
    }
}
