using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    public ENoteType Type { get; }
    public int StartBeat { get; }
    public int Length { get; }
    public int Pitch { get; }
    public string Text { get; }

    public int MidiNote
    {
        get
        {
            // MIDI_Pitch = 60, is middle c, is 0 in lyrics txt file.
            return Pitch + 60;
        }
    }

    public int EndBeat
    {
        get
        {
            return StartBeat + Length;
        }
    }

    public bool IsGolden
    {
        get
        {
            return Type == ENoteType.Golden || Type == ENoteType.RapGolden;
        }
    }

    public bool IsNormal
    {
        get
        {
            return Type == ENoteType.Normal || Type == ENoteType.Rap;
        }
    }

    public bool IsFreestyle
    {
        get
        {
            return Type == ENoteType.Freestyle;
        }
    }

    public Note(ENoteType type, int startBeat, int length, int pitch, string text)
    {
        if (length < 1)
        {
            throw new UnityException("Illegal note length " + length + " at note starting at beat " + startBeat);
        }
        Type = type;
        StartBeat = startBeat;
        Length = length;
        Pitch = pitch;
        Text = text;
    }
}
