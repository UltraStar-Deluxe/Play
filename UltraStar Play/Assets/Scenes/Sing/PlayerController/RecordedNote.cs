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

    public Note TargetNote { get; set; }

    public RecordedNote(int recordedMidiNote, int roundedMidiNote, double startBeat, double endBeat, Note targetNote)
    {
        RecordedMidiNote = recordedMidiNote;
        RoundedMidiNote = roundedMidiNote;
        StartBeat = startBeat;
        EndBeat = endBeat;
        TargetNote = targetNote;
    }
}
