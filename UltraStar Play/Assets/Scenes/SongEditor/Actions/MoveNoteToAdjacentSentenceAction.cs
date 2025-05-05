using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNoteToAdjacentSentenceAction : INeedInjection
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    public bool CanMoveToNextSentence(List<Note> selectedNotes, Note targetNote)
    {
        if (selectedNotes.IsNullOrEmpty())
        {
            return false;
        }

        // Check that the selected notes are the last notes in the sentence.
        Sentence currentSentence = selectedNotes.FirstOrDefault().Sentence;
        if (currentSentence == null)
        {
            return false;
        }

        int minBeat = selectedNotes.Min(note => note.StartBeat);
        bool areLastInSentence = currentSentence.Notes.AllMatch(note => note.EndBeat < minBeat || selectedNotes.Contains(note));
        if (!areLastInSentence)
        {
            return false;
        }

        // Check that there exists a following sentence
        Sentence nextSentence = GetNextSentence(currentSentence);
        return (nextSentence != null);
    }

    public bool CanMoveToPreviousSentence(List<Note> selectedNotes, Note targetNote)
    {
        if (selectedNotes.IsNullOrEmpty())
        {
            return false;
        }

        // Check that the selected notes are the first notes in the sentence.
        Sentence currentSentence = selectedNotes.FirstOrDefault().Sentence;
        if (currentSentence == null)
        {
            return false;
        }

        int maxBeat = selectedNotes.Max(note => note.EndBeat);
        bool areFirstInSentence = currentSentence.Notes.AllMatch(note => note.StartBeat > maxBeat || selectedNotes.Contains(note));
        if (!areFirstInSentence)
        {
            return false;
        }

        // Check that there exists a previous sentence
        Sentence previousSentence = GetPreviousSentence(currentSentence);
        return (previousSentence != null);
    }

    public void MoveToPreviousSentence(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return;
        }

        Sentence oldSentence = notes.FirstOrDefault().Sentence;
        if (oldSentence == null)
        {
            return;
        }

        Sentence previousSentence = GetPreviousSentence(oldSentence);
        SongEditorSongMetaUtils.AddTrailingSpaceToLastNoteOfSentence(oldSentence);
        SongEditorSongMetaUtils.AddTrailingSpaceToLastNoteOfSentence(previousSentence);
        notes.ForEach(note => note.SetSentence(previousSentence));

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

    public void MoveToPreviousSentenceAndNotify(List<Note> notes)
    {
        MoveToPreviousSentence(notes);
        songMetaChangedEventStream.OnNext(new SentencesChangedEvent());
    }

    public void MoveToNextSentence(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return;
        }

        Sentence oldSentence = notes.FirstOrDefault().Sentence;
        if (oldSentence == null)
        {
            return;
        }

        Sentence nextSentence = GetNextSentence(oldSentence);
        SongEditorSongMetaUtils.AddTrailingSpaceToLastNoteOfSentence(oldSentence);
        SongEditorSongMetaUtils.AddTrailingSpaceToLastNoteOfSentence(nextSentence);
        notes.ForEach(note => note.SetSentence(nextSentence));

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

    public void MoveToNextSentenceAndNotify(List<Note> notes)
    {
        MoveToNextSentence(notes);
        songMetaChangedEventStream.OnNext(new SentencesChangedEvent());
    }

    private static Sentence GetNextSentence(Sentence sentence)
    {
        if (sentence.Voice == null)
        {
            return null;
        }

        List<Sentence> sortedSentencesOfVoice = new(sentence.Voice.Sentences);
        sortedSentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sortedSentencesOfVoice)
        {
            if (lastSentence == sentence)
            {
                return s;
            }
            lastSentence = s;
        }
        return null;
    }

    private static Sentence GetPreviousSentence(Sentence sentence)
    {
        if (sentence.Voice == null)
        {
            return null;
        }

        List<Sentence> sortedSentencesOfVoice = new(sentence.Voice.Sentences);
        sortedSentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sortedSentencesOfVoice)
        {
            if (s == sentence)
            {
                return lastSentence;
            }
            lastSentence = s;
        }
        return null;
    }
}
