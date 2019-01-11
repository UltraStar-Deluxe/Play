using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sentence
{
    // this needs to be switched over to IReadOnlyList
    public List<Note> Notes {get;}
    public uint LinebreakBeat {get;}

    public Sentence(List<Note> notes, uint linebreakBeat)
    {
        if (notes == null || notes.Count < 1)
        {
            throw new UnityException("notes is null or empty!");
        }
        Notes = notes;
        LinebreakBeat = linebreakBeat;
    }

    public uint GetStartBeat()
    {
        return Notes[0].StartBeat;
    }

    public uint GetEndBeat()
    {
        Note lastNote = Notes[Notes.Count-1];
        return lastNote.StartBeat + lastNote.Length;
    }
}
