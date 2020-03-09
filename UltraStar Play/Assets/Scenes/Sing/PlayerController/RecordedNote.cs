using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class RecordedNote
{
    public int RecordedMidiNote { get; set; }
    public int RoundedMidiNote { get; set; }

    public double StartBeat { get; set; }
    public double EndBeat { get; set; }

    public Sentence Sentence { get; set; }
    public Note TargetNote { get; set; }

    public RecordedNote(int midiNote, double startBeat, double endBeat)
    {
        this.RecordedMidiNote = midiNote;
        this.RoundedMidiNote = midiNote;
        this.StartBeat = startBeat;
        this.EndBeat = endBeat;
    }
}