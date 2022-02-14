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
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaSelectionDragListener : MonoBehaviour, INeedInjection, IDragListener<NoteAreaDragEvent>
{
    [InjectedInInspector]
    public RectTransform selectionFrame;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    [Inject]
    private Canvas canvas;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongMeta songMeta;

    [Inject(UxmlName = R.UxmlNames.noteAreaSelectionFrame)]
    private VisualElement noteAreaSelectionFrame;

    private readonly float scrollBorderPercent = 0.05f;

    private bool isCanceled;

    private Vector2 scrollAmount;
    private float lastScrollVerticalTime;

    private NoteAreaDragEvent startDragEvent;
    private NoteAreaDragEvent lastDragEvent;

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
        if (dragEvent.GeneralDragEvent.InputButton != (int)PointerEventData.InputButton.Left)
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

        noteAreaSelectionFrame.ShowByDisplay();
        noteAreaSelectionFrame.style.width = 0;
        noteAreaSelectionFrame.style.height = 0;
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
        noteAreaSelectionFrame.HideByDisplay();
    }

    public void CancelDrag()
    {
        scrollAmount = Vector2.zero;
        selectionFrame.gameObject.SetActive(false);
        noteAreaSelectionFrame.HideByDisplay();
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void UpdateSelection(NoteAreaDragEvent currentDragEvent)
    {
        int startBeat = GetDragStartBeat();
        int endBeat = GetDragEndBeat(currentDragEvent);
        int startMidiNote = GetDragStartMidiNote();
        int endMidiNote = GetDragEndMidiNote(currentDragEvent);

        List<Note> visibleNotes = songEditorSceneControl.GetAllVisibleNotes();
        List<Note> selectedNotes = visibleNotes
            .Where(note => IsInSelectionFrame(note, startMidiNote, endMidiNote, startBeat, endBeat))
            .ToList();

        // Add to selection via Shift. Remove from selection via Ctrl+Shift. Without modifier, set selection.
        if (InputUtils.IsKeyboardShiftPressed())
        {
            if (InputUtils.IsKeyboardControlPressed())
            {
                selectionControl.RemoveFromSelection(selectedNotes);
            }
            else
            {
                selectionControl.AddToSelection(selectedNotes);
            }
        }
        else
        {
            selectionControl.SetSelection(selectedNotes);
        }
    }

    private int GetDragStartBeat()
    {
        return startDragEvent.PositionInSongInBeatsDragStart;
    }

    private int GetDragEndBeat(NoteAreaDragEvent currentDragEvent)
    {
        return noteArea.PixelsToBeat(currentDragEvent.GeneralDragEvent.ScreenCoordinateInPixels.CurrentPosition.x);
    }

    private int GetDragStartMidiNote()
    {
        return startDragEvent.MidiNoteDragStart;
    }

    private int GetDragEndMidiNote(NoteAreaDragEvent currentDragEvent)
    {
        return noteArea.PixelsToMidiNote(currentDragEvent.GeneralDragEvent.ScreenCoordinateInPixels.CurrentPosition.y);
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

    private void UpdateSelectionFrame(NoteAreaDragEvent currentDragEvent)
    {
        Vector3 canvasScale = canvas.transform.localScale;
        if (canvasScale.x == 0 || canvasScale.y == 0)
        {
            return;
        }

        // Coordinates in milliseconds and midi-note
        int startBeat = GetDragStartBeat();
        int endBeat = GetDragEndBeat(currentDragEvent);
        int startMidiNote = GetDragStartMidiNote();
        int endMidiNote = GetDragEndMidiNote(currentDragEvent);

        // Min and max coordinates in pixels
        float minX = noteArea.BeatToPixels(noteArea.MinBeatInViewport);
        float maxX = noteArea.BeatToPixels(noteArea.MinBeatInViewport + noteArea.ViewportWidthInBeats);
        float minY = noteArea.MidiNoteToPixels(noteArea.MinMidiNoteInViewport);
        float maxY = noteArea.MidiNoteToPixels(noteArea.MinMidiNoteInViewport + noteArea.ViewportHeight);

        // Calculate selection frame start
        float fromX = noteArea.BeatToPixels(startBeat);
        fromX = NumberUtils.Limit(fromX, minX, maxX);
        float fromY = noteArea.MidiNoteToPixels(startMidiNote);
        fromY = NumberUtils.Limit(fromY, minY, maxY);

        // Calculate selection frame end
        float toX = noteArea.BeatToPixels(endBeat);
        toX = NumberUtils.Limit(toX, minX, maxX);
        float toY = noteArea.MidiNoteToPixels(endMidiNote);
        toY = NumberUtils.Limit(toY, minY, maxY);

        if (toX < fromX)
        {
            ObjectUtils.Swap(ref toX, ref fromX);
        }
        if (toY < fromY)
        {
            ObjectUtils.Swap(ref toY, ref fromY);
        }

        float width = (toX - fromX) / canvasScale.x;
        float height = (toY - fromY) / canvasScale.y;

        selectionFrame.position = new Vector2(fromX, fromY);
        selectionFrame.sizeDelta = new Vector2(width, height);

        int fromBeat = Mathf.Min(startBeat, endBeat);
        int toBeat = Mathf.Max(startBeat, endBeat);
        int fromMidiNote = Mathf.Min(startMidiNote, endMidiNote);
        int toMidiNote = Mathf.Max(startMidiNote, endMidiNote);

        float xPercent = (float)noteArea.GetHorizontalPositionForBeat(fromBeat);
        float yPercent = (float)noteArea.GetVerticalPositionForMidiNote(fromMidiNote);
        float widthPercent = (float)(toBeat - fromBeat) / noteArea.ViewportWidthInBeats;
        float heightPercent = (float)(toMidiNote - fromMidiNote) / noteArea.ViewportHeight;
        noteAreaSelectionFrame.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
        noteAreaSelectionFrame.style.bottom = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        noteAreaSelectionFrame.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
        noteAreaSelectionFrame.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
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
        }

        if (scrollAmount.y != 0
            && lastScrollVerticalTime + 0.1f < Time.time)
        {
            lastScrollVerticalTime = Time.time;
            noteArea.SetViewportY(noteArea.ViewportY + (int)(scrollAmount.y));
        }

        if (scrollAmount.x != 0
            || scrollAmount.y != 0)
        {
            UpdateSelectionFrame(lastDragEvent);
            UpdateSelection(lastDragEvent);
        }
    }
}
