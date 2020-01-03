using System.Collections.Generic;

public class NoteTypeChangeEvent : ISongMetaChangeEvent
{
    private IReadOnlyCollection<Note> notes;
    private ENoteType newType;

    public NoteTypeChangeEvent(IReadOnlyCollection<Note> notes, ENoteType newType)
    {
        this.notes = notes;
        this.newType = newType;
    }
}