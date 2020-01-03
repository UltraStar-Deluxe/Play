using System.Collections.Generic;

public class NotesDeletedEvent : ISongMetaChangeEvent
{
    private IReadOnlyCollection<Note> notes;

    public NotesDeletedEvent(IReadOnlyCollection<Note> notes)
    {
        this.notes = notes;
    }
}