using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaSelectionDragListener : INeedInjection, IInjectionFinishedListener, IDragListener<NoteAreaDragEvent>
{
    public static readonly ReactiveProperty<NoteAreaRect> lastSelectionRect = new();

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private NoteAreaDragControl noteAreaDragControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMeta songMeta;
    
    [Inject(UxmlName = R.UxmlNames.noteAreaSentences)]
    private VisualElement noteAreaSentences;

    [Inject(UxmlName = R.UxmlNames.noteAreaSelectionFrame)]
    private VisualElement noteAreaSelectionFrame;
    
    [Inject(UxmlName = R.UxmlNames.lastNoteAreaSelectionFrame)]
    private VisualElement lastNoteAreaSelectionFrame;

    private bool isCanceled;

    private Vector2 scrollAmount;
    private float lastScrollVerticalTime;
    
    private NoteAreaDragEvent lastDragEvent;

    public void OnInjectionFinished()
    {
        noteAreaDragControl.AddListener(this);
        
        lastNoteAreaSelectionFrame.HideByDisplay();
        lastNoteAreaSelectionFrame.style.width = 0;
        
        // Update selection rect
        noteAreaControl.ViewportEventStream.Subscribe(evt =>
        {
            if (lastSelectionRect.Value == null)
            {
                return;
            }
            UpdateSelectionFrame(lastSelectionRect.Value, lastNoteAreaSelectionFrame, false, true);
        });
        lastSelectionRect
            .Subscribe(_ =>  UpdateSelectionFrame(lastSelectionRect.Value, lastNoteAreaSelectionFrame, false, true));
        selectionControl.NoteSelectionChangedEventStream
            .Subscribe(newSelection =>
            {
                if (newSelection.SelectedNotes.IsNullOrEmpty())
                {
                    return;
                }
                int minBeat = newSelection.SelectedNotes.Min(note => note.StartBeat);
                int maxBeat = newSelection.SelectedNotes.Max(note => note.EndBeat);
                int minMidiNote = newSelection.SelectedNotes.Min(note => note.MidiNote);
                int maxMidiNote = newSelection.SelectedNotes.Max(note => note.MidiNote);
                lastSelectionRect.Value = NoteAreaRect.CreateFromBeats(songMeta, minBeat, maxBeat, minMidiNote, maxMidiNote);
            });
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;

        if (dragEvent.GeneralDragEvent.InputButton != (int)PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        // Check whether this is a drag gesture to manipulate notes, not to select notes
        Vector2 dragStartPositionInPx = dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition;
        if (editorNoteDisplayer.AnyNoteControlContainsPosition(dragStartPositionInPx))
        {
            CancelDrag();
            return;
        }

        // Check whether this is a drag gesture to manipulate sentences, not to select notes
        if (editorNoteDisplayer.AnySentenceControlContainsPosition(dragStartPositionInPx))
        {
            CancelDrag();
            return;
        }
        
        // Check whether this is a drag gesture to draw notes, not to select notes
        if (selectionControl.IsSelectionEmpty
            && InputUtils.IsKeyboardShiftPressed())
        {
            CancelDrag();
            return;
        }

        noteAreaSelectionFrame.ShowByDisplay();
        lastNoteAreaSelectionFrame.ShowByDisplay();
        noteAreaSelectionFrame.style.width = 0;
        noteAreaSelectionFrame.style.height = 0;
        lastDragEvent = dragEvent;
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        lastDragEvent = dragEvent;
        UpdateSelectionFrames(dragEvent);
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

    private void UpdateSelection(NoteAreaDragEvent dragEvent)
    {
        int startBeat = GetDragStartBeat(dragEvent);
        int endBeat = GetDragEndBeat(dragEvent);
        int startMidiNote = GetDragStartMidiNote(dragEvent);
        int endMidiNote = GetDragEndMidiNote(dragEvent);

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

    private int GetDragStartBeat(NoteAreaDragEvent dragEvent)
    {
        return dragEvent.PositionInBeatsDragStart;
    }

    private int GetDragEndBeat(NoteAreaDragEvent dragEvent)
    {
        return noteAreaControl.ScreenPixelPositionToBeat(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.CurrentPosition.x);
    }

    private int GetDragStartMidiNote(NoteAreaDragEvent dragEvent)
    {
        return dragEvent.MidiNoteDragStart;
    }

    private int GetDragEndMidiNote(NoteAreaDragEvent dragEvent)
    {
        return noteAreaControl.ScreenPixelPositionToMidiNote(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.CurrentPosition.y);
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

        double[] beatIntersection = NumberUtils.GetIntersection(startBeat, endBeat, note.StartBeat, note.EndBeat);
        return !beatIntersection.IsNullOrEmpty()
               && (startMidiNote <= note.MidiNote && note.MidiNote <= endMidiNote)
               && Mathf.Abs(startMidiNote - endMidiNote) > 0;
    }

    private void UpdateSelectionFrames(NoteAreaDragEvent dragEvent)
    {
        lastSelectionRect.Value = NoteAreaRect.CreateFromBeats(songMeta,
            GetDragStartBeat(dragEvent),
            GetDragEndBeat(dragEvent),
            GetDragStartMidiNote(dragEvent),
            GetDragEndMidiNote(dragEvent));

        UpdateSelectionFrame(lastSelectionRect.Value, noteAreaSelectionFrame, true, true);
        UpdateSelectionFrame(lastSelectionRect.Value, lastNoteAreaSelectionFrame, false, true);
    }
    
    private void UpdateSelectionFrame(
        NoteAreaRect selectionRect,
        VisualElement selectionFrameElement,
        bool vertical,
        bool horizontal)
    {
        if (selectionRect == null)
        {
            selectionFrameElement.HideByVisibility();
            return;
        }
        selectionFrameElement.ShowByVisibility();
        
        int startBeat = selectionRect.MinBeat;
        int endBeat = selectionRect.MaxBeat;
        
        int startMidiNote = selectionRect.MinMidiNote;
        int endMidiNote = selectionRect.MaxMidiNote;
        
        if (horizontal)
        {
            int fromBeat = Mathf.Min(startBeat, endBeat);
            int toBeat = Mathf.Max(startBeat, endBeat);
            float xPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(fromBeat);
            float widthPercent = (float)(toBeat - fromBeat) / noteAreaControl.ViewportWidthInBeats;
            selectionFrameElement.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
            selectionFrameElement.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
        }

        if (vertical)
        {
            int fromMidiNote = Mathf.Min(startMidiNote, endMidiNote);
            int toMidiNote = Mathf.Max(startMidiNote, endMidiNote);
            float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(fromMidiNote);
            float heightPercent = (float)(toMidiNote - fromMidiNote) / noteAreaControl.ViewportHeight;
            selectionFrameElement.style.top = new StyleLength(new Length((yPercent - heightPercent) * 100, LengthUnit.Percent));
            selectionFrameElement.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
        }
    }

    private void UpdateScrollAmount(NoteAreaDragEvent dragEvent)
    {
        int scrollAmountX = (int)(noteAreaControl.ViewportWidth * Time.deltaTime);
        scrollAmountX += (int)(Math.Abs(dragEvent.GeneralDragEvent.LocalCoordinateInPercent.Distance.x) - NoteAreaControl.ViewportAutomaticScrollingBoarderPercent) * 1000;

        int scrollAmountY = 1;

        // X-Coordinate
        if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.x > (1 - NoteAreaControl.ViewportAutomaticScrollingBoarderPercent))
        {
            scrollAmount = new Vector2(scrollAmountX, scrollAmount.y);
        }
        else if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.x < NoteAreaControl.ViewportAutomaticScrollingBoarderPercent)
        {
            scrollAmount = new Vector2(-scrollAmountX, scrollAmount.y);
        }
        else
        {
            scrollAmount = new Vector2(0, scrollAmount.y);
        }

        // Y-Coordinate
        if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.y > (1 - NoteAreaControl.ViewportAutomaticScrollingBoarderPercent))
        {
            scrollAmount = new Vector2(scrollAmount.x, scrollAmountY);
        }
        else if (dragEvent.GeneralDragEvent.LocalCoordinateInPercent.CurrentPosition.y < NoteAreaControl.ViewportAutomaticScrollingBoarderPercent)
        {
            scrollAmount = new Vector2(scrollAmount.x, -scrollAmountY);
        }
        else
        {
            scrollAmount = new Vector2(scrollAmount.x, 0);
        }
    }

    public void Update()
    {
        UpdateAutoScroll();
    }
    
    private void UpdateAutoScroll()
    {
        if (scrollAmount.x != 0)
        {
            noteAreaControl.SetViewportX(noteAreaControl.ViewportX + (int)(scrollAmount.x));
        }

        if (scrollAmount.y != 0
            && lastScrollVerticalTime + 0.1f < Time.time)
        {
            lastScrollVerticalTime = Time.time;
            noteAreaControl.SetViewportY(noteAreaControl.ViewportY - (int)(scrollAmount.y));
        }

        if (scrollAmount.x != 0
            || scrollAmount.y != 0)
        {
            UpdateSelectionFrames(lastDragEvent);
            UpdateSelection(lastDragEvent);
        }
    }
}
