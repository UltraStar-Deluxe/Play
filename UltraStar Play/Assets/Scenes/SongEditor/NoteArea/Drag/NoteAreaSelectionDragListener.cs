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

public class NoteAreaSelectionDragListener : INeedInjection, IInjectionFinishedListener, IDragListener<NoteAreaDragEvent>
{
    private static readonly float scrollBorderPercent = 0.05f;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private NoteAreaDragControl noteAreaDragControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongMeta songMeta;

    [Inject(UxmlName = R.UxmlNames.noteAreaSelectionFrame)]
    private VisualElement noteAreaSelectionFrame;

    private bool isCanceled;

    private Vector2 scrollAmount;
    private float lastScrollVerticalTime;

    private NoteAreaDragEvent startDragEvent;
    private NoteAreaDragEvent lastDragEvent;

    public void OnInjectionFinished()
    {
        noteAreaDragControl.AddListener(this);
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
        noteAreaSelectionFrame.HideByDisplay();
    }

    public void CancelDrag()
    {
        scrollAmount = Vector2.zero;
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
        return noteAreaControl.PixelsToBeat(currentDragEvent.GeneralDragEvent.ScreenCoordinateInPixels.CurrentPosition.x);
    }

    private int GetDragStartMidiNote()
    {
        return startDragEvent.MidiNoteDragStart;
    }

    private int GetDragEndMidiNote(NoteAreaDragEvent currentDragEvent)
    {
        return noteAreaControl.PixelsToMidiNote(currentDragEvent.GeneralDragEvent.ScreenCoordinateInPixels.CurrentPosition.y);
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
            && (startMidiNote <= note.MidiNote && note.MidiNote <= endMidiNote)
            && Mathf.Abs(startMidiNote - endMidiNote) > 0;
    }

    private void UpdateSelectionFrame(NoteAreaDragEvent currentDragEvent)
    {
        // float scrollAmountSumXInPx = noteAreaControl.MillisecondsToPixelDistance((int)scrollAmountSum.x);
        // float scrollAmountSumYInPx = noteAreaControl.MidiNotesToPixelDistance((int)scrollAmountSum.y);
        // float startX = currentDragEvent.GeneralDragEvent.LocalCoordinateInPixels.StartPosition.x - scrollAmountSumXInPx;
        // float currentX = currentDragEvent.GeneralDragEvent.LocalCoordinateInPixels.CurrentPosition.x;
        // float startY = currentDragEvent.GeneralDragEvent.LocalCoordinateInPixels.StartPosition.y - scrollAmountSumYInPx;
        // float currentY = currentDragEvent.GeneralDragEvent.LocalCoordinateInPixels.CurrentPosition.y;
        //
        // float minX = Mathf.Min(startX, currentX);
        // float maxX = Mathf.Max(startX, currentX);
        // float minY = Mathf.Min(startY, currentY);
        // float maxY = Mathf.Max(startY, currentY);
        //
        // noteAreaSelectionFrame.style.left = minX;
        // noteAreaSelectionFrame.style.bottom = minY;
        // noteAreaSelectionFrame.style.width = maxX - minX;
        // noteAreaSelectionFrame.style.height = maxY - minY;

        // Coordinates in milliseconds and midi-note
        int startBeat = GetDragStartBeat();
        int endBeat = GetDragEndBeat(currentDragEvent);
        int startMidiNote = GetDragStartMidiNote();
        int endMidiNote = GetDragEndMidiNote(currentDragEvent);

        int fromBeat = Mathf.Min(startBeat, endBeat);
        int toBeat = Mathf.Max(startBeat, endBeat);
        int fromMidiNote = Mathf.Min(startMidiNote, endMidiNote);
        int toMidiNote = Mathf.Max(startMidiNote, endMidiNote);

        float xPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(fromBeat);
        float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(fromMidiNote);
        float widthPercent = (float)(toBeat - fromBeat) / noteAreaControl.ViewportWidthInBeats;
        float heightPercent = (float)(toMidiNote - fromMidiNote) / noteAreaControl.ViewportHeight;
        noteAreaSelectionFrame.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
        noteAreaSelectionFrame.style.bottom = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        noteAreaSelectionFrame.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
        noteAreaSelectionFrame.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
    }

    private void UpdateScrollAmount(NoteAreaDragEvent dragEvent)
    {
        int scrollAmountX = 200;
        scrollAmountX += (int)(Math.Abs(dragEvent.GeneralDragEvent.LocalCoordinateInPercent.Distance.x) - scrollBorderPercent) * 1000;

        int scrollAmountY = 1;

        // X-Coordinate
        if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.x > (1 - scrollBorderPercent))
        {
            scrollAmount = new Vector2(scrollAmountX, scrollAmount.y);
        }
        else if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.x < scrollBorderPercent)
        {
            scrollAmount = new Vector2(-scrollAmountX, scrollAmount.y);
        }
        else
        {
            scrollAmount = new Vector2(0, scrollAmount.y);
        }

        // Y-Coordinate
        if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.y > (1 - scrollBorderPercent))
        {
            scrollAmount = new Vector2(scrollAmount.x, scrollAmountY);
        }
        else if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.y < scrollBorderPercent)
        {
            scrollAmount = new Vector2(scrollAmount.x, -scrollAmountY);
        }
        else
        {
            scrollAmount = new Vector2(scrollAmount.x, 0);
        }
    }

    public void UpdateAutoScroll()
    {
        if (scrollAmount.x != 0)
        {
            noteAreaControl.SetViewportX(noteAreaControl.ViewportX + (int)(scrollAmount.x));
        }

        if (scrollAmount.y != 0
            && lastScrollVerticalTime + 0.1f < Time.time)
        {
            lastScrollVerticalTime = Time.time;
            noteAreaControl.SetViewportY(noteAreaControl.ViewportY + (int)(scrollAmount.y));
        }

        if (scrollAmount.x != 0
            || scrollAmount.y != 0)
        {
            UpdateSelectionFrame(lastDragEvent);
            UpdateSelection(lastDragEvent);
        }
    }
}
