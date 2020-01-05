using System.Collections.Generic;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DeleteNotesAction : INeedInjection
{
    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void Execute(IReadOnlyCollection<Note> selectedNotes)
    {
        foreach (Note note in selectedNotes)
        {
            note.SetSentence(null);
            songEditorLayerManager.RemoveNoteFromAllLayers(note);
            editorNoteDisplayer.DeleteNote(note);
        }
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes)
    {
        Execute(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesDeletedEvent());
    }
}