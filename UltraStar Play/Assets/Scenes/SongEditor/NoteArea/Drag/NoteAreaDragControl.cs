using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaDragControl : AbstractDragControl<NoteAreaDragEvent>
{
    [Inject]
    private NoteAreaControl noteAreaControl;

    protected override NoteAreaDragEvent CreateDragEventStart(DragControlPointerEvent eventData)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEventStart(eventData);

        int midiNoteDragStart = (int)(noteAreaControl.ViewportY + generalDragEvent.LocalCoordinateInPercent.StartPosition.y * noteAreaControl.ViewportHeight);
        int midiNoteDistance = 0;

        int positionInSongInMillisDragStart = (int)(noteAreaControl.ViewportX + generalDragEvent.LocalCoordinateInPercent.StartPosition.x * noteAreaControl.ViewportWidth);
        int millisDistance = 0;

        int positionInSongInBeatsDragStart = (int)(noteAreaControl.MinBeatInViewport + generalDragEvent.LocalCoordinateInPercent.StartPosition.x * noteAreaControl.ViewportWidthInBeats);
        int beatDistance = 0;

        NoteAreaDragEvent result = new NoteAreaDragEvent(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance);

        return result;
    }

    protected override NoteAreaDragEvent CreateDragEvent(DragControlPointerEvent eventData, NoteAreaDragEvent dragStartEvent)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEvent(eventData, dragStartEvent.GeneralDragEvent);

        int midiNoteDragStart = dragStartEvent.MidiNoteDragStart;
        int midiNoteDistance = (int)(generalDragEvent.LocalCoordinateInPercent.Distance.y * noteAreaControl.ViewportHeight);

        int positionInSongInMillisDragStart = dragStartEvent.PositionInSongInMillisDragStart;
        int millisDistance = (int)(generalDragEvent.LocalCoordinateInPercent.Distance.x * noteAreaControl.ViewportWidth);

        int positionInSongInBeatsDragStart = dragStartEvent.PositionInSongInBeatsDragStart;
        int beatDistance = (int)(generalDragEvent.LocalCoordinateInPercent.Distance.x * noteAreaControl.ViewportWidthInBeats);

        NoteAreaDragEvent result = new NoteAreaDragEvent(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance);

        return result;
    }
}
