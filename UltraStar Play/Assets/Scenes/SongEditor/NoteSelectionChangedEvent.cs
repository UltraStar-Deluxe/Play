using System.Collections.Generic;

public class NoteSelectionChangedEvent
{
    public IReadOnlyCollection<Note> SelectedNotes { get; private set; }

    public NoteSelectionChangedEvent(IReadOnlyCollection<Note> selectedNotes)
    {
        SelectedNotes = selectedNotes;
    }
}
