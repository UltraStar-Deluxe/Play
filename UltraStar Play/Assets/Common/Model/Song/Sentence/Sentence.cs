using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Sentence : ISerializationCallbackReceiver
{
    public readonly static IComparer<Sentence> comparerByStartBeat = new SentenceComparerByStartBeat();

    // Breaks the serialization loop with Voice.sentences. The field is restored by the Voice.
    [NonSerialized]
    private Voice voice;
    public Voice Voice { get { return voice; } }

    // LinebreakBeat can be set to extend the duration the sentence is shown.
    // Must be greater than the MaxBeat and smaller or equal to the MinBeat of the following sentence.
    public int LinebreakBeat { get; private set; }

    private readonly NoteHashSet notes = new NoteHashSet();
    public IReadOnlyCollection<Note> Notes { get { return notes; } }

    // The following fields are computed from the list of notes.
    public int MinBeat { get; private set; }
    public int MaxBeat { get; private set; }

    // ExtendedMaxBeat is equal to Math.Max(MaxBeat, LinebreakBeat)
    public int ExtendedMaxBeat { get; private set; }

    public Sentence()
    {
    }

    public Sentence(int minBeat, int maxBeat)
    {
        MinBeat = minBeat;
        MaxBeat = maxBeat;
        LinebreakBeat = maxBeat;
        ExtendedMaxBeat = maxBeat;
    }

    public Sentence(List<Note> notes, int linebreakBeat = 0)
    {
        LinebreakBeat = linebreakBeat;
        ExtendedMaxBeat = linebreakBeat;
        SetNotes(notes);
    }

    public void SetVoice(Voice voice)
    {
        if (this.voice == voice)
        {
            return;
        }

        if (this.voice != null)
        {
            this.voice.RemoveSentence(this);
        }
        this.voice = voice;
        if (this.voice != null)
        {
            this.voice.AddSentence(this);
        }
    }

    public void UpdateMinAndMaxBeat()
    {
        UpdateMinBeat();
        UpdateMaxBeat();
    }

    public void SetNotes(List<Note> newNotes)
    {
        if (newNotes == null)
        {
            throw new ArgumentNullException("Notes cannot be null!");
        }

        foreach (Note note in new List<Note>(notes))
        {
            RemoveNote(note);
        }

        foreach (Note note in newNotes)
        {
            this.notes.Add(note);
            note.SetSentence(this);
        }

        UpdateMinAndMaxBeat();
    }

    public void AddNote(Note note)
    {
        if (note == null)
        {
            throw new ArgumentNullException("Note cannot be null");
        }

        // The check is needed to avoid a recursive loop between Sentence.AddNote and Note.SetSentence.
        if (notes.Contains(note))
        {
            return;
        }
        notes.Add(note);
        note.SetSentence(this);

        UpdateMinAndMaxBeat();
    }

    public void RemoveNote(Note note)
    {
        if (note == null)
        {
            throw new UnityException("Note cannot be null");
        }

        // The check is needed to avoid a recursive loop between Sentence.RemoveNote and Note.SetSentence.
        if (!notes.Contains(note))
        {
            return;
        }
        notes.Remove(note);
        note.SetSentence(null);

        if (MinBeat == note.StartBeat)
        {
            UpdateMinBeat();
        }
        if (MaxBeat == note.EndBeat)
        {
            UpdateMaxBeat();
        }
    }

    public void FitToNotes()
    {
        UpdateMinAndMaxBeat();
        SetLinebreakBeat(MaxBeat);
    }

    public void SetLinebreakBeat(int newBeat)
    {
        LinebreakBeat = Math.Max(MaxBeat, newBeat);
        ExtendedMaxBeat = Math.Max(MaxBeat, LinebreakBeat);
    }

    public void UpdateMinBeat()
    {
        if (notes.Count > 0)
        {
            MinBeat = notes.Select(it => it.StartBeat).Min();
        }
    }

    public void UpdateMaxBeat()
    {
        if (notes.Count > 0)
        {
            MaxBeat = notes.Select(it => it.EndBeat).Max();
            if (LinebreakBeat < MaxBeat)
            {
                LinebreakBeat = MaxBeat;
                ExtendedMaxBeat = MaxBeat;
            }
        }
    }

    public void OnBeforeSerialize()
    {
        // Do nothing. Implementation of ISerializationCallbackReceiver
    }

    public void OnAfterDeserialize()
    {
        foreach (Note note in notes)
        {
            note.SetSentence(this);
        }
    }

    public Sentence CloneDeep()
    {
        List<Note> notesCopy = new List<Note>();
        foreach (Note note in notes)
        {
            Note noteCopy = note.Clone();
            notesCopy.Add(noteCopy);
        }

        Sentence clone = new Sentence(notesCopy, LinebreakBeat);
        return clone;
    }

    private class SentenceComparerByStartBeat : IComparer<Sentence>
    {
        public int Compare(Sentence x, Sentence y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }

            return x.MinBeat.CompareTo(y.MinBeat);
        }
    }
}
