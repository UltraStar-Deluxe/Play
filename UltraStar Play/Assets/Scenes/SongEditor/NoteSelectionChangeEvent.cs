using System.Collections.Generic;

public class NoteSelectionChangeEvent
{
    public IReadOnlyCollection<Note> SelectedNotes { get; private set; }

    public NoteSelectionChangeEvent(IReadOnlyCollection<Note> selectedNotes)
    {
        SelectedNotes = selectedNotes;
    }
}
