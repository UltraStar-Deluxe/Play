using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNoteToAjacentSentenceAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public bool CanMoveToNextSentence(List<Note> selectedNotes, Note targetNote)
    {
        if (selectedNotes.Count != 1)
        {
            return false;
        }

        Note selectedNote = selectedNotes[0];
        if (selectedNote != targetNote || selectedNote.Sentence == null)
        {
            return false;
        }

        // Check that the selected note is the last note in the sentence.
        List<Note> notesInSentence = new List<Note>(selectedNote.Sentence.Notes);
        notesInSentence.Sort(Note.comparerByStartBeat);
        if (notesInSentence.Last() != selectedNote)
        {
            return false;
        }

        // Check that there exists a following sentence
        Sentence nextSentence = SongMetaUtils.GetNextSentence(selectedNote.Sentence);
        return (nextSentence != null);
    }

    public bool CanMoveToPreviousSentence(List<Note> selectedNotes, Note targetNote)
    {
        if (selectedNotes.Count != 1)
        {
            return false;
        }

        Note selectedNote = selectedNotes[0];
        if (selectedNote != targetNote || selectedNote.Sentence == null)
        {
            return false;
        }

        // Check that the selected note is the first note in the sentence.
        List<Note> notesInSentence = new List<Note>(selectedNote.Sentence.Notes);
        notesInSentence.Sort(Note.comparerByStartBeat);
        if (notesInSentence.First() != selectedNote)
        {
            return false;
        }

        // Check that there exists a previous sentence
        Sentence previousSentence = SongMetaUtils.GetPreviousSentence(selectedNote.Sentence);
        return (previousSentence != null);
    }

    public void MoveToPreviousSentence(Note targetNote)
    {
        Sentence oldSentence = targetNote.Sentence;

        Sentence previousSentence = SongMetaUtils.GetPreviousSentence(targetNote.Sentence);
        targetNote.SetSentence(previousSentence);

        // Remove old sentence if not more notes left
        if (oldSentence.Notes.Count == 0)
        {
            oldSentence.SetVoice(null);
        }
        else
        {
            oldSentence.FitToNotes();
        }
    }

    public void MoveToPreviousSentenceAndNotify(Note targetNote)
    {
        MoveToPreviousSentence(targetNote);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }

    public void MoveToNextSentence(Note targetNote)
    {
        Sentence oldSentence = targetNote.Sentence;

        Sentence nextSentence = SongMetaUtils.GetNextSentence(targetNote.Sentence);
        targetNote.SetSentence(nextSentence);

        // Remove old sentence if not more notes left
        if (oldSentence.Notes.Count == 0)
        {
            oldSentence.SetVoice(null);
        }
        else
        {
            oldSentence.FitToNotes();
        }
    }

    public void MoveToNextSentenceAndNotify(Note targetNote)
    {
        MoveToNextSentence(targetNote);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }
}