using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    public ENoteType Type {get;}
    public uint StartBeat {get;}
    public uint Length {get;}
    public int Pitch {get;}
    public string Text {get;}

    public uint EndBeat {
        get {
            return StartBeat + Length;
        }
    }
    
    public Note(ENoteType type, uint startBeat, uint length, int pitch, string text)
    {
        if (length < 1)
        {
            throw new UnityException("Illegal note length "+length+" at note starting at beat "+startBeat);
        }
        Type = type;
        StartBeat = startBeat;
        Length = length;
        Pitch = pitch;
        Text = text;
    }
}
