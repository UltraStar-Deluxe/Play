using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNoteToOwnSentenceAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;
    
    [Inject]
    private DeleteSentencesAction deleteSentencesAction;
    
    public bool CanMoveToOwnSentence(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return false;
        }
        return notes.AnyMatch(note => note.Sentence?.Voice != null);
    }

    public void MoveToOwnSentence(List<Note> notes)
    {
        List<Sentence> affectedSentences = notes.Select(note => note.Sentence).ToList();

        Sentence newSentence = new Sentence();
        Voice voice = notes
            .Select(note => note.Sentence?.Voice)
            .FirstOrDefault();
        newSentence.SetVoice(voice);
        
        notes.ForEach(note => note.SetSentence(newSentence));
        newSentence.FitToNotes();
        
        // Remove old sentence if not more notes left
        List<Sentence> sentencesWithoutNotes = affectedSentences.Where(it => it.Notes.IsNullOrEmpty()).ToList();
        deleteSentencesAction.Execute(sentencesWithoutNotes);

        // Fit sentences to notes
        List<Sentence> sentencesWithNotes = affectedSentences.Where(it => !it.Notes.IsNullOrEmpty()).ToList();
        sentencesWithNotes.ForEach(it => it.FitToNotes());
    }

    public void MoveToOwnSentenceAndNotify(List<Note> notes)
    {
        MoveToOwnSentence(notes);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }
}
