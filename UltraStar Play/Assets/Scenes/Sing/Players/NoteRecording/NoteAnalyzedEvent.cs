public class NoteAnalyzedEvent
{
    public Note Note { get; private set; }

    public NoteAnalyzedEvent(Note note)
    {
        Note = note;
    }
}
