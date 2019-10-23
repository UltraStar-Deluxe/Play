using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sentence
{
    // this needs to be switched over to IReadOnlyList
    public List<Note> Notes { get; }
    public int LinebreakBeat { get; }

    public List<Note> GoldenNotes
    {
        get
        {
            return Notes.Where(it => it.IsGolden).ToList();
        }
    }

    public List<Note> NormalNotes
    {
        get
        {
            return Notes.Where(it => it.IsNormal).ToList();
        }
    }

    public List<Note> FreestyleNotes
    {
        get
        {
            return Notes.Where(it => it.IsFreestyle).ToList();
        }
    }

    public Sentence(List<Note> notes, int linebreakBeat)
    {
        if (notes == null || notes.Count < 1)
        {
            throw new UnityException("notes is null or empty!");
        }
        Notes = notes;
        LinebreakBeat = linebreakBeat;

        // Calculate values based on note (e.g. min/max pitch, start/end beat)
        Note firstNote = Notes[0];
        StartBeat = firstNote.StartBeat;

        Note lastNote = Notes[Notes.Count - 1];
        EndBeat = lastNote.StartBeat + lastNote.Length;

        MinNote = firstNote;
        MaxNote = firstNote;
        foreach (Note note in notes)
        {
            if (note.MidiNote < MinNote.MidiNote)
            {
                MinNote = note;
            }
            if (note.MidiNote > MaxNote.MidiNote)
            {
                MaxNote = note;
            }
        }
        AvgMidiNote = Notes.Select(it => it.MidiNote).Average();
    }

    public int StartBeat { get; private set; }

    public int EndBeat { get; private set; }

    public Note MinNote { get; private set; }

    public Note MaxNote { get; private set; }

    public double AvgMidiNote { get; private set; }
}
