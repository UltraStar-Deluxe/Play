using System.Collections.Generic;

public class NotesDeletedEvent : SongMetaChangedEvent
{
    public IReadOnlyCollection<Note> Notes { get; set; }
}
