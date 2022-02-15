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
    
    public List<Sentence> Execute(IReadOnlyCollection<Note> selectedNotes)
    {
        if (selectedNotes.IsNullOrEmpty())
        {
            return new List<Sentence>();
        }

        HashSet<Sentence> affectedSentences = new HashSet<Sentence>();
        foreach (Note note in selectedNotes)
        {
            if (note.Sentence != null)
            {
                affectedSentences.Add(note.Sentence);
            }
            note.SetSentence(null);
            songEditorLayerManager.RemoveNoteFromAllLayers(note);
            editorNoteDisplayer.DeleteNoteControl(note);
        }

        List<Sentence> affectedSentencesWithoutNotes = affectedSentences
            .Where(sentence => sentence.Notes.IsNullOrEmpty())
            .ToList();
        deleteSentencesAction.Execute(affectedSentencesWithoutNotes);
        return affectedSentencesWithoutNotes;
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes)
    {
        List<Sentence> deletedSentences = Execute(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesDeletedEvent
        {
            Notes = selectedNotes
        });

        if (!deletedSentences.IsNullOrEmpty())
        {
            songMetaChangeEventStream.OnNext(new SentencesDeletedEvent
            {
                Sentences = deletedSentences
            });
        }
    }
}
