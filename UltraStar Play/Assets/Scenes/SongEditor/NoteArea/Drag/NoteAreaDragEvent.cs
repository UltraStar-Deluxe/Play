using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NoteAreaDragEvent
{
    public GeneralDragEvent GeneralDragEvent { get; private set; }

    public int MidiNoteDragStart { get; private set; }
    public int MidiNoteDistance { get; private set; }

    public int PositionInSongInMillisDragStart { get; private set; }
    public int MillisDistance { get; private set; }

    public int PositionInSongInBeatsDragStart { get; private set; }
    public int BeatDistance { get; private set; }

    public NoteAreaDragEvent(GeneralDragEvent generalDragEvent,
        int midiNoteDragStart, int midiNoteDistance,
        int positionInSongInMillisDragStart, int millisDistance,
        int positionInSongInBeatsDragStart, int beatDistance)
    {
        GeneralDragEvent = generalDragEvent;

        MidiNoteDragStart = midiNoteDragStart;
        MidiNoteDistance = midiNoteDistance;

        PositionInSongInMillisDragStart = positionInSongInMillisDragStart;
        MillisDistance = millisDistance;

        PositionInSongInBeatsDragStart = positionInSongInBeatsDragStart;
        BeatDistance = beatDistance;
    }
}