using System.Collections.Generic;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ToggleNoteTypeAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void Execute(IEnumerable<Note> selectedNotes, ENoteType noteType)
    {
        bool allHaveNoteType = selectedNotes.AllMatch(it => it.Type == noteType);
        ENoteType targetNoteType = (allHaveNoteType) ? ENoteType.Normal : noteType;
        foreach (Note note in selectedNotes)
        {
            note.SetType(targetNoteType);
        }
    }

    public void ExecuteAndNotify(IEnumerable<Note> selectedNotes, ENoteType noteType)
    {
        Execute(selectedNotes, noteType);
        songMetaChangeEventStream.OnNext(new NoteTypeChangeEvent());
    }
}