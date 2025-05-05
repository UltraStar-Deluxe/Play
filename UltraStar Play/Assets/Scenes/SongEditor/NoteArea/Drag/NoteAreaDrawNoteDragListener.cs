using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaDrawNoteDragListener : INeedInjection, IInjectionFinishedListener, IDragListener<NoteAreaDragEvent>
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private CursorManager cursorManager;

    [Inject]
    private NoteAreaDragControl noteAreaDragControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private MoveNotesToOtherVoiceAction moveNotesToOtherVoiceAction;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    private bool isCanceled;
    private NoteAreaDragEvent lastDragEvent;

    private Note noteUnderConstruction;

    public void OnInjectionFinished()
    {
        noteAreaDragControl.AddListener(this);

        noteAreaControl.VisualElement.RegisterCallback<KeyDownEvent>(_ => UpdateCursor());
        noteAreaControl.VisualElement.RegisterCallback<KeyUpEvent>(_ => UpdateCursor());
        noteAreaControl.VisualElement.RegisterCallback<PointerMoveEvent>(_ => UpdateCursor());
        noteAreaControl.VisualElement.RegisterCallback<PointerEnterEvent>(_ => UpdateCursor());
        noteAreaControl.VisualElement.RegisterCallback<PointerLeaveEvent>(_ => UpdateCursor());
    }

    private void UpdateCursor()
    {
        if (noteAreaControl.IsPointerOver()
            && InputUtils.IsKeyboardShiftPressed()
            && selectionControl.IsSelectionEmpty)
        {
            cursorManager.SetCursorPencil();
        }
        else if (cursorManager.CurrentCursor == ECursor.Pencil)
        {
            cursorManager.SetDefaultCursor();
        }
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;

        if (dragEvent.GeneralDragEvent.InputButton != (int)PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        // Check whether this is a drag gesture to manipulate notes, not to draw notes
        Vector2 dragStartPositionInPx = dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition;
        if (editorNoteDisplayer.AnyNoteControlContainsPosition(dragStartPositionInPx))
        {
            CancelDrag();
            return;
        }

        // Check whether this is a drag gesture to manipulate sentences, not to draw notes
        if (editorNoteDisplayer.AnySentenceControlContainsPosition(dragStartPositionInPx))
        {
            CancelDrag();
            return;
        }

        // Check whether this is a drag gesture to extend the selection, not to draw notes
        if (!(selectionControl.IsSelectionEmpty
            && InputUtils.IsKeyboardShiftPressed()))
        {
            CancelDrag();
            return;
        }

        lastDragEvent = dragEvent;
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        lastDragEvent = dragEvent;

        UpdateNoteUnderConstruction(dragEvent);

        if (Touch.activeTouches.Count > 1)
        {
            CancelDrag();
        }
    }

    private void UpdateNoteUnderConstruction(NoteAreaDragEvent dragEvent)
    {
        int midiNote = dragEvent.MidiNoteDragStart;

        // Keep the note-under-construction a little bit before the cursor to avoid pointer events.
        int reducedDistanceInBeats = NumberUtils.Towards(dragEvent.BeatDistance, 0, 1);
        int startBeat;
        int endBeat;
        if (reducedDistanceInBeats >= 0)
        {
            startBeat = dragEvent.PositionInBeatsDragStart;
            endBeat = dragEvent.PositionInBeatsDragStart + reducedDistanceInBeats;
        }
        else
        {
            startBeat = dragEvent.PositionInBeatsDragStart + reducedDistanceInBeats;
            endBeat = dragEvent.PositionInBeatsDragStart;
        }

        int lengthInBeats = endBeat - startBeat;
        if (lengthInBeats < 1
            && noteUnderConstruction == null)
        {
            return;
        }

        if (noteUnderConstruction == null)
        {
            noteUnderConstruction = new Note(ENoteType.Normal,
                startBeat,
                lengthInBeats,
                MidiUtils.GetUltraStarTxtPitch(midiNote),
                "");

            layerManager.AddNoteToEnumLayer(ESongEditorLayer.ButtonRecording, noteUnderConstruction);
            if (settings.SongEditorSettings.DrawNoteLayer == ESongEditorDrawNoteLayer.P1)
            {
                moveNotesToOtherVoiceAction.MoveNotesToVoice(songMeta, new List<Note>() { noteUnderConstruction }, EVoiceId.P1, false);
                editorNoteDisplayer.ReloadSentences();
            }
            else if (settings.SongEditorSettings.DrawNoteLayer == ESongEditorDrawNoteLayer.P2)
            {
                moveNotesToOtherVoiceAction.MoveNotesToVoice(songMeta, new List<Note>() { noteUnderConstruction }, EVoiceId.P2, false);
                editorNoteDisplayer.ReloadSentences();
            }
        }
        else if (startBeat != noteUnderConstruction.StartBeat
                 || endBeat != noteUnderConstruction.EndBeat)
        {
            noteUnderConstruction.SetStartAndEndBeat(startBeat, endBeat);
        }

        editorNoteDisplayer.UpdateNotes();
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        lastDragEvent = dragEvent;
        UpdateNoteUnderConstruction(dragEvent);
        FinishNoteUnderConstruction();
    }

    private void FinishNoteUnderConstruction()
    {
        if (noteUnderConstruction == null)
        {
            return;
        }

        noteUnderConstruction = null;
        songMetaChangedEventStream.OnNext(new NotesChangedEvent());
    }

    public void CancelDrag()
    {
        RemoveNoteUnderConstruction();
        isCanceled = true;
    }

    private void RemoveNoteUnderConstruction()
    {
        if (noteUnderConstruction == null)
        {
            return;
        }

        layerManager.RemoveNoteFromAllEnumLayers(noteUnderConstruction);
        noteUnderConstruction.SetSentence(null);
        editorNoteDisplayer.RemoveNoteControl(noteUnderConstruction);
        noteUnderConstruction = null;
    }

    public bool IsCanceled()
    {
        return isCanceled;
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
}
