using System.Collections.Generic;

public class ImportedMidiFileEvent : ImportedNotesEvent
{
    public ImportedMidiFileEvent(List<Note> notes) : base(notes)
    {
    }
}
