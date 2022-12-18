using System;
using System.Collections.Generic;
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

    /**
     * The first beat (inclusive) where this note is positioned.
     */
    public int StartBeat { get; private set; }

    /**
     * The last beat (exclusive) where this note is positioned.
     * Note that the EndBeat is exclusive, i.e. the note is NOT drawn there.
     * Example: a note only on beat 1 has StartBeat==1, Length==1, EndBeat==2.
     */
    public int EndBeat { get; private set; }

    /**
     * The number of beats that this note occupies.
     */
    public int Length { get; private set; }

    // A Pitch of 0 in song txt file is middle C, which is 60 (C4) as MIDI note.
    public int TxtPitch { get; private set; }
    public int MidiNote { get; private set; }
    public string Text { get; private set; }

    public bool IsGolden { get; private set; }
    public bool IsNormal { get; private set; }
    public bool IsFreestyle { get; private set; }
    public bool IsRap { get; private set; }

    // Is the note editable in the song editor?
    public bool IsEditable { get; set; } = true;

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
        Note clone = new();
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
        IsRap = other.IsRap;
        Text = other.Text;
        IsEditable = other.IsEditable;
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

    public void SetTxtPitch(int txtPitch)
    {
        TxtPitch = txtPitch;
        MidiNote = MidiUtils.GetMidiNotePitch(TxtPitch);
    }

    public void SetMidiNote(int midiNote)
    {
        MidiNote = midiNote;
        TxtPitch = MidiUtils.GetUltraStarTxtPitch(MidiNote);
    }

    public void SetType(ENoteType type)
    {
        Type = type;
        IsGolden = (Type == ENoteType.Golden || Type == ENoteType.RapGolden);
        IsNormal = (Type == ENoteType.Normal || Type == ENoteType.Rap);
        IsFreestyle = (Type == ENoteType.Freestyle);
        IsRap = (Type == ENoteType.Rap || Type == ENoteType.RapGolden);
    }

    public void SetStartBeat(int newStartBeat)
    {
        if (newStartBeat > EndBeat)
        {
            throw new UnityException("StartBeat must be less or equal to EndBeat");
        }

        if (newStartBeat == EndBeat)
        {
            newStartBeat = EndBeat - 1;
        }

        if (StartBeat != newStartBeat)
        {
            StartBeat = newStartBeat;
            Length = EndBeat - StartBeat;

            // Update the sentence's min beat
            if (Sentence != null)
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

        if (newEndBeat == StartBeat)
        {
            newEndBeat = StartBeat + 1;
        }

        if (EndBeat != newEndBeat)
        {
            EndBeat = newEndBeat;
            Length = EndBeat - StartBeat;

            // Update the sentence's max beat
            if (Sentence != null)
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

        if (newLength == 0)
        {
            newLength = 1;
        }

        if (Length != newLength)
        {
            Length = newLength;
            EndBeat = StartBeat + Length;

            // Update the sentence's max beat
            if (Sentence != null)
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
            StartBeat = newStartBeat;
            EndBeat = newEndBeat;

            if (StartBeat == EndBeat)
            {
                EndBeat = StartBeat + 1;
            }

            Length = EndBeat - StartBeat;

            // Update the sentence's min and max beat
            if (Sentence != null)
            {
                Sentence.UpdateMinBeat();
                Sentence.UpdateMaxBeat();
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
