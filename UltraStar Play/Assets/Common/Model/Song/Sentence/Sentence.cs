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

        Note firstNote = Notes[0];
        StartBeat = firstNote.StartBeat;

        Note lastNote = Notes[Notes.Count-1];
        EndBeat = lastNote.StartBeat + lastNote.Length;
    }

    public uint StartBeat { get; internal set; }

    public uint EndBeat { get; internal set; }
}
