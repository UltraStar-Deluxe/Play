using System.Collections.Generic;

public class NotesSplitEvent : ISongMetaChangeEvent
{
    private IReadOnlyCollection<Note> notes;

    public NotesSplitEvent(IReadOnlyCollection<Note> notes)
    {
        this.notes = notes;
    }
}