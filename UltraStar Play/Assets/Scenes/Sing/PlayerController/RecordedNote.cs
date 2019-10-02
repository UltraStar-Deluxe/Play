using System;
using System.Collections;
using System.Collections.Generic;
using Pitch;

[Serializable]
public class RecordedNote
{
    public int RecordedMidiNote { get; set; }
    public int RoundedMidiNote { get; set; }

    public double StartPositionInMilliseconds { get; set; }
    public double EndPositionInMilliseconds { get; set; }

    public double StartBeat { get; set; }
    public double EndBeat { get; set; }

    public RecordedNote(int midiNote, double startPositionInMilliseconds, double endPositionInMilliseconds, double startBeat, double endBeat)
    {
        this.RecordedMidiNote = midiNote;
        this.RoundedMidiNote = midiNote;
        this.StartPositionInMilliseconds = startPositionInMilliseconds;
        this.EndPositionInMilliseconds = endPositionInMilliseconds;
        this.StartBeat = startBeat;
        this.EndBeat = endBeat;
    }

    public double LengthInBeats
    {
        get
        {
            return EndBeat - StartBeat;
        }
    }

    public double LengthInMilliseconds
    {
        get
        {
            return EndPositionInMilliseconds - StartPositionInMilliseconds;
        }
    }
}