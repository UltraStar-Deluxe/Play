using System.Collections.Generic;

public class ImportedNotesEvent : NotesAddedEvent
{
    public List<Note> Notes { get; private set; }

    public ImportedNotesEvent(List<Note> notes)
    {
        Notes = notes;
    }
}
