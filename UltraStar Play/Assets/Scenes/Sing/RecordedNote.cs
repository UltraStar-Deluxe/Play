using System;
using System.Collections;
using System.Collections.Generic;
using Pitch;

[Serializable]
public class RecordedNote
{
    public int midiNote;
    public double startPositionInMilliseconds;
    public double endPositionInMilliseconds;

    public RecordedNote(int midiNote, double startPositionInMilliseconds, double endPositionInMilliseconds)
    {
        this.midiNote = midiNote;
        this.startPositionInMilliseconds = startPositionInMilliseconds;
        this.endPositionInMilliseconds = endPositionInMilliseconds;
    }
}