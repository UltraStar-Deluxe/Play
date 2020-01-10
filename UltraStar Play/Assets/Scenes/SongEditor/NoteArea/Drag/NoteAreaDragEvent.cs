using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NoteAreaDragEvent
{
    public PointerEventData.InputButton InputButton { get; private set; }

    public float XDragStartInPixels { get; private set; }
    public float YDragStartInPixels { get; private set; }

    public float YDistanceInPixels { get; private set; }
    public float XDistanceInPixels { get; private set; }

    public Vector2 DragStartPositionInPixels { get; private set; }

    public int MidiNoteDragStart { get; private set; }
    public int MidiNoteDistance { get; private set; }

    public int PositionInSongInMillisDragStart { get; private set; }
    public int MillisDistance { get; private set; }

    public int PositionInSongInBeatsDragStart { get; private set; }
    public int BeatDistance { get; private set; }

    public List<RaycastResult> RaycastResultsDragStart { get; private set; }

    public NoteAreaDragEvent(float xDragStartInPixels, float yDragStartInPixels,
        float xDistanceInPixels, float yDistanceInPixels,
        int midiNoteDragStart, int midiNoteDistance,
        int positionInSongInMillisDragStart, int millisDistance,
        int positionInSongInBeatsDragStart, int beatDistance,
        List<RaycastResult> raycastResultsDragStart,
        PointerEventData.InputButton inputButton)
    {
        XDragStartInPixels = xDragStartInPixels;
        YDragStartInPixels = yDragStartInPixels;
        XDistanceInPixels = xDistanceInPixels;
        YDistanceInPixels = yDistanceInPixels;
        MidiNoteDragStart = midiNoteDragStart;
        MidiNoteDistance = midiNoteDistance;
        PositionInSongInMillisDragStart = positionInSongInMillisDragStart;
        MillisDistance = millisDistance;
        PositionInSongInBeatsDragStart = positionInSongInBeatsDragStart;
        BeatDistance = beatDistance;
        RaycastResultsDragStart = raycastResultsDragStart;

        DragStartPositionInPixels = new Vector2(xDragStartInPixels, yDragStartInPixels);

        InputButton = inputButton;
    }
}