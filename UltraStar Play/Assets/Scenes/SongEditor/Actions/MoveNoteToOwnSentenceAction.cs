using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNoteToOwnSentenceAction : INeedInjection
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

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
        List<Sentence> affectedSentences = notes
            .Where(note => note.Sentence != null)
            .Select(note => note.Sentence)
            .ToList();

        Voice affectedVoice = notes
            .Select(note => note.Sentence?.Voice)
            .FirstOrDefault();

        Sentence newSentence = new();
        newSentence.SetVoice(affectedVoice);

        notes.ForEach(note =>
        {
            // Prevent notes from merging into a single word
            SongEditorSongMetaUtils.AddTrailingSpaceToLastNoteOfSentence(note);

            note.SetSentence(newSentence);
        });
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
        songMetaChangedEventStream.OnNext(new SentencesChangedEvent());
    }
}
