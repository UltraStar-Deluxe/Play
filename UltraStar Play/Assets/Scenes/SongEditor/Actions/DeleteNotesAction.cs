using System.Collections.Generic;
using System.Linq;
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

    [Inject]
    private DeleteSentencesAction deleteSentencesAction;
    
    public void Execute(IReadOnlyCollection<Note> selectedNotes)
    {
        HashSet<Sentence> affectedSentences = new HashSet<Sentence>();
        foreach (Note note in selectedNotes)
        {
            if (note.Sentence != null)
            {
                affectedSentences.Add(note.Sentence);
            }
            note.SetSentence(null);
            songEditorLayerManager.RemoveNoteFromAllLayers(note);
            editorNoteDisplayer.DeleteNote(note);
        }

        List<Sentence> affectedSentencesWithoutNotes = affectedSentences
            .Where(sentence => sentence.Notes.Count == 0)
            .ToList();
        deleteSentencesAction.Execute(affectedSentencesWithoutNotes);
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes)
    {
        Execute(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesDeletedEvent());
    }
}
