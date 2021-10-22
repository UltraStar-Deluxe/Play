using System.Collections.Generic;

public class NotesDeletedEvent : SongMetaChangeEvent
{
    public IReadOnlyCollection<Note> Notes { get; set; }
}
