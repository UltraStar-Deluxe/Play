using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MergeSentencesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public bool CanExecute(IReadOnlyCollection<Note> selectedNotes)
    {
        return selectedNotes.Count > 1 && selectedNotes.Select(it => it.Sentence).Distinct().Count() > 1;
    }

    public void Execute(IReadOnlyCollection<Note> selectedNotes, Note targetNote)
    {
        List<Sentence> oldSentences = selectedNotes.Select(note => note.Sentence).Distinct().ToList();
        Sentence targetSentence = targetNote.Sentence;
        if (targetSentence == null)
        {
            targetSentence = oldSentences.FirstOrDefault();
        }

        foreach (Note note in selectedNotes)
        {
            note.SetSentence(targetSentence);
        }

        // Remove old and now unused sentences.
        foreach (Sentence oldSentence in oldSentences)
        {
            if (oldSentence.Notes.Count == 0)
            {
                oldSentence.SetVoice(null);
            }
        }
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes, Note targetNote)
    {
        Execute(selectedNotes, targetNote);
        songMetaChangeEventStream.OnNext(new SentencesMergedEvent());
    }
}