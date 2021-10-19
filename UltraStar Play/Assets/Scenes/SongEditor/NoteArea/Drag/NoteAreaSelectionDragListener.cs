using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaSelectionDragListener : MonoBehaviour, INeedInjection, IDragListener<NoteAreaDragEvent>
{
    [InjectedInInspector]
    public RectTransform selectionFrame;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    [Inject]
    private Canvas canvas;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private SongMeta songMeta;

    private float scrollBorderPercent = 0.05f;

    private bool isCanceled;

    private Vector2 scrollAmount;
    private float lastScrollVerticalTime;

    private NoteAreaDragEvent startDragEvent;
    private NoteAreaDragEvent lastDragEvent;
    private Vector2 scrollDistanceSinceDragStart;

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    void Update()
    {
        UpdateScroll();
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
        lastDragEvent = dragEvent;
        startDragEvent = dragEvent;
        scrollDistanceSinceDragStart = Vector2.zero;
        if (dragEvent.GeneralDragEvent.InputButton != PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        GameObject raycastTarget = dragEvent.GeneralDragEvent.RaycastResultsDragStart.Select(it => it.gameObject).FirstOrDefault();
        if (raycastTarget != noteArea.gameObject)
        {
            CancelDrag();
            return;
        }

        selectionFrame.gameObject.SetActive(true);
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        lastDragEvent = dragEvent;
        UpdateSelectionFrame(dragEvent);
        UpdateSelection(dragEvent);

        UpdateScrollAmount(dragEvent);

        if (Touch.activeTouches.Count > 1)
        {
            CancelDrag();
        }
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        lastDragEvent = dragEvent;
        scrollAmount = Vector2.zero;
        selectionFrame.gameObject.SetActive(false);
    }

    public void CancelDrag()
    {
        scrollAmount = Vector2.zero;
        selectionFrame.gameObject.SetActive(false);
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void UpdateSelection(NoteAreaDragEvent currentDragEvent)
    {
        int startBeat = startDragEvent.PositionInSongInBeatsDragStart;
        int endBeat = startBeat + currentDragEvent.BeatDistance + (int) (scrollDistanceSinceDragStart.x / BpmUtils.MillisecondsPerBeat(songMeta));

        int startMidiNote = startDragEvent.MidiNoteDragStart;
        int endMidiNote = startMidiNote + currentDragEvent.MidiNoteDistance + ((int) scrollDistanceSinceDragStart.y);

        List<Note> visibleNotes = songEditorSceneController.GetAllVisibleNotes();
        List<Note> selectedNotes = visibleNotes
            .Where(note => IsInSelectionFrame(note, startMidiNote, endMidiNote, startBeat, endBeat))
            .ToList();

        // Add to selection via Shift. Remove from selection via Ctrl+Shift. Without modifier, set selection.
        if (InputUtils.IsKeyboardShiftPressed())
        {
            if (InputUtils.IsKeyboardControlPressed())
            {
                selectionController.RemoveFromSelection(selectedNotes);
            }
            else
            {
                selectionController.AddToSelection(selectedNotes);
            }
        }
        else
        {
            selectionController.SetSelection(selectedNotes);
        }
    }

    private bool IsInSelectionFrame(
        Note note,
        int startMidiNote,
        int endMidiNote,
        int startBeat,
        int endBeat)
    {
        if (note == null)
        {
            return false;
        }

        if (startMidiNote > endMidiNote)
        {
            ObjectUtils.Swap(ref startMidiNote, ref endMidiNote);
        }

        if (startBeat > endBeat)
        {
            ObjectUtils.Swap(ref startBeat, ref endBeat);
        }

        return (startBeat <= note.StartBeat && note.EndBeat <= endBeat)
            && (startMidiNote <= note.MidiNote && note.MidiNote <= endMidiNote);
    }

    private void UpdateSelectionFrame(NoteAreaDragEvent dragEvent)
    {
        Vector3 canvasScale = canvas.transform.localScale;
        if (canvasScale.x == 0 || canvasScale.y == 0)
        {
            return;
        }
        float x = dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition.x;
        float y = dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition.y;
        float width = dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.Distance.x / canvasScale.x;
        float height = -dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.Distance.y / canvasScale.y;

        if (width < 0)
        {
            width = -width;
            x -= (width * canvasScale.x);
        }
        if (height < 0)
        {
            height = -height;
            y += (height * canvasScale.x);
        }
        selectionFrame.position = new Vector2(x, y);
        selectionFrame.sizeDelta = new Vector2(width, height);
    }

    private void UpdateScrollAmount(NoteAreaDragEvent dragEvent)
    {
        int scrollAmountX = 200;
        scrollAmountX += (int)(Math.Abs(dragEvent.GeneralDragEvent.RectTransformCoordinateInPercent.Distance.x) - scrollBorderPercent) * 1000;

        int scrollAmountY = 1;

        // X-Coordinate
        if (dragEvent.GeneralDragEvent.RectTransformCoordinateInPercent.CurrentPosition.x > (1 - scrollBorderPercent))
        {
            scrollAmount = new Vector2(scrollAmountX, scrollAmount.y);
        }
        else if (dragEvent.GeneralDragEvent.RectTransformCoordinateInPercent.CurrentPosition.x < scrollBorderPercent)
        {
            scrollAmount = new Vector2(-scrollAmountX, scrollAmount.y);
        }
        else
        {
            scrollAmount = new Vector2(0, scrollAmount.y);
        }

        // Y-Coordinate
        if (dragEvent.GeneralDragEvent.RectTransformCoordinateInPercent.CurrentPosition.y > (1 - scrollBorderPercent))
        {
            scrollAmount = new Vector2(scrollAmount.x, scrollAmountY);
        }
        else if (dragEvent.GeneralDragEvent.RectTransformCoordinateInPercent.CurrentPosition.y < scrollBorderPercent)
        {
            scrollAmount = new Vector2(scrollAmount.x, -scrollAmountY);
        }
        else
        {
            scrollAmount = new Vector2(scrollAmount.x, 0);
        }
    }

    private void UpdateScroll()
    {
        if (scrollAmount.x != 0)
        {
            noteArea.SetViewportX(noteArea.ViewportX + (int)(scrollAmount.x));
            scrollDistanceSinceDragStart = new Vector2(scrollDistanceSinceDragStart.x + scrollAmount.x, scrollDistanceSinceDragStart.y);
        }

        if (scrollAmount.y != 0
            && lastScrollVerticalTime + 0.1f < Time.time)
        {
            lastScrollVerticalTime = Time.time;
            noteArea.SetViewportY(noteArea.ViewportY + (int)(scrollAmount.y));
            scrollDistanceSinceDragStart = new Vector2(scrollDistanceSinceDragStart.x, scrollDistanceSinceDragStart.y + scrollAmount.y);
        }

        if (scrollAmount.x != 0
            || scrollAmount.y != 0)
        {
            UpdateSelectionFrame(lastDragEvent);
            UpdateSelection(lastDragEvent);
        }
    }
}
