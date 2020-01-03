using System.Collections.Generic;

public class NotesMergedEvent : ISongMetaChangeEvent
{
    private IReadOnlyCollection<Note> originalNotes;
    private Note mergedNote;

    public NotesMergedEvent(IReadOnlyCollection<Note> originalNotes, Note mergedNote)
    {
        this.originalNotes = originalNotes;
        this.mergedNote = mergedNote;
    }
}