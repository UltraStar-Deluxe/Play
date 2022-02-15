using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaDragHandler : AbstractDragHandler<NoteAreaDragEvent>
{
    [Inject]
    private NoteAreaControl noteAreaControl;

    void Start()
    {
    }

    protected override NoteAreaDragEvent CreateDragEventStart(PointerEventData eventData)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEventStart(eventData);

        int midiNoteDragStart = (int)(noteAreaControl.ViewportY + generalDragEvent.RectTransformCoordinateInPercent.StartPosition.y * noteAreaControl.ViewportHeight);
        int midiNoteDistance = 0;

        int positionInSongInMillisDragStart = (int)(noteAreaControl.ViewportX + generalDragEvent.RectTransformCoordinateInPercent.StartPosition.x * noteAreaControl.ViewportWidth);
        int millisDistance = 0;

        int positionInSongInBeatsDragStart = (int)(noteAreaControl.MinBeatInViewport + generalDragEvent.RectTransformCoordinateInPercent.StartPosition.x * noteAreaControl.ViewportWidthInBeats);
        int beatDistance = 0;

        NoteAreaDragEvent result = new NoteAreaDragEvent(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance);

        return result;
    }

    protected override NoteAreaDragEvent CreateDragEvent(PointerEventData eventData, NoteAreaDragEvent dragStartEvent)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEvent(eventData, dragStartEvent.GeneralDragEvent);

        int midiNoteDragStart = dragStartEvent.MidiNoteDragStart;
        int midiNoteDistance = (int)(generalDragEvent.RectTransformCoordinateInPercent.Distance.y * noteAreaControl.ViewportHeight);

        int positionInSongInMillisDragStart = dragStartEvent.PositionInSongInMillisDragStart;
        int millisDistance = (int)(generalDragEvent.RectTransformCoordinateInPercent.Distance.x * noteAreaControl.ViewportWidth);

        int positionInSongInBeatsDragStart = dragStartEvent.PositionInSongInBeatsDragStart;
        int beatDistance = (int)(generalDragEvent.RectTransformCoordinateInPercent.Distance.x * noteAreaControl.ViewportWidthInBeats);

        NoteAreaDragEvent result = new NoteAreaDragEvent(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance);

        return result;
    }
}
