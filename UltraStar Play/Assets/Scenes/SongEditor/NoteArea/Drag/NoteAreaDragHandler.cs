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

public class NoteAreaDragHandler : MonoBehaviour, INeedInjection, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private readonly List<INoteAreaDragListener> dragListeners = new List<INoteAreaDragListener>();

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Canvas canvas;

    [Inject]
    private GraphicRaycaster graphicRaycaster;

    private bool isDragging;
    private bool ignoreDrag;

    private NoteAreaDragEvent dragStartEvent;

    private RectTransform noteAreaRectTransform;

    void Start()
    {
        noteAreaRectTransform = noteArea.GetComponent<RectTransform>();
    }

    void Update()
    {
        // Cancel drag via Escape key
        if (isDragging && Input.GetKeyUp(KeyCode.Escape))
        {
            CancelDrag();
        }
    }

    public void AddListener(INoteAreaDragListener listener)
    {
        dragListeners.Add(listener);
    }

    public void RemoveListener(INoteAreaDragListener listener)
    {
        dragListeners.Remove(listener);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ignoreDrag = false;
        isDragging = true;
        dragStartEvent = CreateNoteAreaBeginDragEvent(eventData);
        NotifyListeners(listener => listener.OnBeginDrag(dragStartEvent), true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ignoreDrag)
        {
            return;
        }

        NoteAreaDragEvent dragEvent = CreateNoteAreaDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnDrag(dragEvent), false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ignoreDrag)
        {
            return;
        }

        NoteAreaDragEvent dragEvent = CreateNoteAreaDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnEndDrag(dragEvent), false);
        isDragging = false;
    }

    private void CancelDrag()
    {
        if (ignoreDrag)
        {
            return;
        }

        isDragging = false;
        ignoreDrag = true;
        NotifyListeners(listener => listener.CancelDrag(), false);
    }

    private void NotifyListeners(Action<INoteAreaDragListener> action, bool includeCanceledListeners)
    {
        foreach (INoteAreaDragListener listener in dragListeners)
        {
            if (includeCanceledListeners || !listener.IsCanceled())
            {
                action(listener);
            }
        }
    }

    private NoteAreaDragEvent CreateNoteAreaDragEvent(PointerEventData eventData, NoteAreaDragEvent dragStartEvent)
    {
        float xDragStartInPixels = dragStartEvent.XDragStartInPixels;
        float yDragStartInPixels = dragStartEvent.YDragStartInPixels;
        float xDistanceInPixels = eventData.position.x - xDragStartInPixels;
        float yDistanceInPixels = eventData.position.y - yDragStartInPixels;

        List<RaycastResult> raycastResults = dragStartEvent.RaycastResultsDragStart;

        float noteAreaWidthInPixels = noteAreaRectTransform.rect.width;
        float noteAreaHeightInPixels = noteAreaRectTransform.rect.height;
        double xDistancePercent = xDistanceInPixels / noteAreaWidthInPixels;
        double yDistancePercent = yDistanceInPixels / noteAreaHeightInPixels;

        int midiNoteDragStart = dragStartEvent.MidiNoteDragStart;
        int midiNoteDistance = (int)(yDistancePercent * noteArea.ViewportHeight);

        int positionInSongInMillisDragStart = dragStartEvent.PositionInSongInMillisDragStart;
        int millisDistance = (int)(xDistancePercent * noteArea.ViewportWidth);

        int positionInSongInBeatsDragStart = dragStartEvent.PositionInSongInBeatsDragStart;
        int beatDistance = (int)(xDistancePercent * noteArea.ViewportWidthInBeats);

        NoteAreaDragEvent result = new NoteAreaDragEvent(xDragStartInPixels, yDragStartInPixels,
            xDistanceInPixels, yDistanceInPixels,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance,
            raycastResults,
            eventData.button);
        return result;
    }

    private NoteAreaDragEvent CreateNoteAreaBeginDragEvent(PointerEventData eventData)
    {
        float xDragStartInPixels = eventData.pressPosition.x;
        float yDragStartInPixels = eventData.pressPosition.y;
        float xDistanceInPixels = 0;
        float yDistanceInPixels = 0;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        PointerEventData eventDataForRaycast = new PointerEventData(EventSystem.current);
        eventDataForRaycast.position = eventData.pressPosition;
        graphicRaycaster.Raycast(eventDataForRaycast, raycastResults);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(noteAreaRectTransform,
                                                                eventData.pressPosition,
                                                                eventData.pressEventCamera,
                                                                out Vector2 localPoint);

        float noteAreaWidthInPixels = noteAreaRectTransform.rect.width;
        float noteAreaHeightInPixels = noteAreaRectTransform.rect.height;
        double xPercent = (localPoint.x + (noteAreaWidthInPixels / 2)) / noteAreaWidthInPixels;
        double yPercent = (localPoint.y + (noteAreaHeightInPixels / 2)) / noteAreaHeightInPixels;

        int midiNoteDragStart = (int)(noteArea.ViewportY + yPercent * noteArea.ViewportHeight);
        int midiNoteDistance = 0;

        int positionInSongInMillisDragStart = (int)(noteArea.ViewportX + xPercent * noteArea.ViewportWidth);
        int millisDistance = 0;

        int positionInSongInBeatsDragStart = (int)(noteArea.MinBeatInViewport + xPercent * noteArea.ViewportWidthInBeats);
        int beatDistance = 0;

        NoteAreaDragEvent result = new NoteAreaDragEvent(xDragStartInPixels, yDragStartInPixels,
            xDistanceInPixels, yDistanceInPixels,
            midiNoteDragStart, midiNoteDistance,
            positionInSongInMillisDragStart, millisDistance,
            positionInSongInBeatsDragStart, beatDistance,
            raycastResults,
            eventData.button);
        return result;
    }
}
