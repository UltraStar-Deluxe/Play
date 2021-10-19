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
    private NoteArea noteArea;

    [Inject]
    private Canvas canvas;
    
    void Start()
    {
        targetRectTransform = noteArea.GetComponent<RectTransform>();
    }

    protected override NoteAreaDragEvent CreateDragEventStart(PointerEventData eventData)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEventStart(eventData);

        int midiNoteDragStart = (int)(noteArea.ViewportY + generalDragEvent.RectTransformCoordinateInPercent.StartPosition.y * noteArea.ViewportHeight);
        int midiNoteDistance = 0;

        int positionInSongInMillisDragStart = (int)(noteArea.ViewportX + generalDragEvent.RectTransformCoordinateInPercent.StartPosition.x * noteArea.ViewportWidth);
        int millisDistance = 0;

        int positionInSongInBeatsDragStart = (int)(noteArea.MinBeatInViewport + generalDragEvent.RectTransformCoordinateInPercent.StartPosition.x * noteArea.ViewportWidthInBeats);
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

        Vector3 canvasScale = canvas.transform.localScale; 
        
        int midiNoteDragStart = dragStartEvent.MidiNoteDragStart;
        int midiNoteDistance = (int)(generalDragEvent.RectTransformCoordinateInPercent.Distance.y * noteArea.ViewportHeight);

        int positionInSongInMillisDragStart = dragStartEvent.PositionInSongInMillisDragStart;
        int millisDistance = (int)(generalDragEvent.RectTransformCoordinateInPercent.Distance.x * noteArea.ViewportWidth);

        int positionInSongInBeatsDragStart = dragStartEvent.PositionInSongInBeatsDragStart;
        int beatDistance = (int)(generalDragEvent.RectTransformCoordinateInPercent.Distance.x * noteArea.ViewportWidthInBeats);

        NoteAreaDragEvent result = new NoteAreaDragEvent(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance);

        return result;
    }
}
