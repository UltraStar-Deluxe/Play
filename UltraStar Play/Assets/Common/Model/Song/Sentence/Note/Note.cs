using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Note
{
    public ENoteType Type { get; private set; }
    public int StartBeat { get; private set; }
    public int EndBeat { get; private set; }
    public int Length { get; private set; }
    public int Pitch { get; private set; }
    public int MidiNote { get; private set; }
    public string Text { get; private set; }

    public bool IsGolden { get; private set; }
    public bool IsNormal { get; private set; }
    public bool IsFreestyle { get; private set; }

    public Note(ENoteType type, int startBeat, int length, int pitch, string text)
    {
        if (length < 1)
        {
            throw new UnityException("Illegal note length " + length + " at note starting at beat " + startBeat);
        }
        Type = type;
        IsGolden = (Type == ENoteType.Golden || Type == ENoteType.RapGolden);
        IsNormal = (Type == ENoteType.Normal || Type == ENoteType.Rap);
        IsFreestyle = (Type == ENoteType.Freestyle);

        StartBeat = startBeat;
        Length = length;
        EndBeat = StartBeat + Length;

        Text = text;

        Pitch = pitch;
        // MIDI_Pitch = 60, is middle c, is 0 in lyrics txt file.
        MidiNote = Pitch + 60;
    }

    public void SetEndBeat(int newEndBeat)
    {
        if (newEndBeat <= StartBeat)
        {
            throw new UnityException("EndBeat must be greater than StartBeat");
        }

        if (EndBeat != newEndBeat)
        {
            EndBeat = newEndBeat;
            Length = EndBeat - StartBeat;
        }
    }
}
