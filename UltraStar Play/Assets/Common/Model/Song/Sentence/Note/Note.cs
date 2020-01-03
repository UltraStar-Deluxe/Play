using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class Note
{
    public readonly static IComparer<Note> comparerByStartBeat = new NoteComparerByStartBeat();

    // Breaks the serialization loop with Sentence.notes. The field is restored by the Sentence.
    [NonSerialized]
    private Sentence sentence;
    public Sentence Sentence { get { return sentence; } }

    public ENoteType Type { get; private set; }
    public int StartBeat { get; private set; }
    public int EndBeat { get; private set; }
    public int Length { get; private set; }
    // A Pitch of 0 in song txt file is middle C, which is 60 (C4) as MIDI note.
    public int TxtPitch { get; private set; }
    public int MidiNote { get; private set; }
    public string Text { get; private set; }

    public bool IsGolden { get; private set; }
    public bool IsNormal { get; private set; }
    public bool IsFreestyle { get; private set; }

    public Note()
    {
        SetType(ENoteType.Normal);
        SetText("");
    }

    public Note(ENoteType type, int startBeat, int length, int txtPitch, string text)
    {
        if (length < 0)
        {
            throw new ArgumentException($"Illegal note length {length} for note starting at beat {startBeat}");
        }
        SetType(type);
        SetText(text);
        SetTxtPitch(txtPitch);

        StartBeat = startBeat;
        Length = length;
        EndBeat = StartBeat + Length;
    }

    public Note Clone()
    {
        Note clone = new Note();
        clone.CopyValues(this);
        return clone;
    }

    public void CopyValues(Note other)
    {
        sentence = other.sentence;
        Type = other.Type;
        StartBeat = other.StartBeat;
        EndBeat = other.EndBeat;
        Length = other.Length;
        TxtPitch = other.TxtPitch;
        MidiNote = other.MidiNote;
        IsNormal = other.IsNormal;
        IsGolden = other.IsGolden;
        IsFreestyle = other.IsFreestyle;
        Text = other.Text;
    }

    public void SetSentence(Sentence sentence)
    {
        if (this.sentence == sentence)
        {
            return;
        }

        if (this.sentence != null)
        {
            this.sentence.RemoveNote(this);
        }
        this.sentence = sentence;
        if (this.sentence != null)
        {
            this.sentence.AddNote(this);
        }
    }

    public void SetText(string text)
    {
        Text = text ?? throw new UnityException("Text cannot be null");
    }

    public void SetTxtPitch(int pitch)
    {
        TxtPitch = pitch;
        MidiNote = TxtPitch + 60;
    }

    public void SetMidiNote(int midiNote)
    {
        MidiNote = midiNote;
        TxtPitch = MidiNote - 60;
    }

    public void SetType(ENoteType type)
    {
        Type = type;
        IsGolden = (Type == ENoteType.Golden || Type == ENoteType.RapGolden);
        IsNormal = (Type == ENoteType.Normal || Type == ENoteType.Rap);
        IsFreestyle = (Type == ENoteType.Freestyle);
    }

    public void SetStartBeat(int newStartBeat)
    {
        if (newStartBeat > EndBeat)
        {
            throw new UnityException("StartBeat must be less or equal to EndBeat");
        }

        if (StartBeat != newStartBeat)
        {
            int oldStartBeat = StartBeat;
            StartBeat = newStartBeat;
            Length = EndBeat - StartBeat;

            // Update the sentence's min beat
            if (Sentence != null && Sentence.MinBeat == oldStartBeat)
            {
                Sentence.UpdateMinBeat();
            }
        }
    }

    public void SetEndBeat(int newEndBeat)
    {
        if (newEndBeat < StartBeat)
        {
            throw new UnityException("EndBeat must be greater or equal to StartBeat");
        }

        if (EndBeat != newEndBeat)
        {
            int oldEndBeat = EndBeat;
            EndBeat = newEndBeat;
            Length = EndBeat - StartBeat;

            // Update the sentence's max beat
            if (Sentence != null && Sentence.MaxBeat == oldEndBeat)
            {
                Sentence.UpdateMaxBeat();
            }
        }
    }

    public void SetLength(int newLength)
    {
        if (newLength < 0)
        {
            throw new UnityException("Length cannot be negative");
        }

        if (Length != newLength)
        {
            int oldEndBeat = EndBeat;
            Length = newLength;
            EndBeat = StartBeat + Length;

            // Update the sentence's max beat
            if (Sentence != null && Sentence.MaxBeat == oldEndBeat)
            {
                Sentence.UpdateMaxBeat();
            }
        }
    }

    public void MoveHorizontal(int distanceInBeats)
    {
        SetStartAndEndBeat(StartBeat + distanceInBeats, EndBeat + distanceInBeats);
    }

    public void MoveVertical(int distanceInMidiNotes)
    {
        SetMidiNote(MidiNote + distanceInMidiNotes);
    }

    public void SetStartAndEndBeat(int newStartBeat, int newEndBeat)
    {
        if (newStartBeat > newEndBeat)
        {
            throw new UnityException("StartBeat cannot be greater than EndBeat");
        }
        if (StartBeat != newStartBeat || EndBeat != newEndBeat)
        {
            int oldStartBeat = StartBeat;
            int oldEndBeat = EndBeat;

            StartBeat = newStartBeat;
            EndBeat = newEndBeat;
            Length = EndBeat - StartBeat;

            // Update the sentence's min and max beat
            if (Sentence != null)
            {
                if (Sentence.MinBeat == oldStartBeat)
                {
                    Sentence.UpdateMinBeat();
                }
                if (Sentence.MaxBeat == oldEndBeat)
                {
                    Sentence.UpdateMaxBeat();
                }
            }
        }
    }

    private class NoteComparerByStartBeat : IComparer<Note>
    {
        public int Compare(Note x, Note y)
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

            return x.StartBeat.CompareTo(y.StartBeat);
        }
    }
}
