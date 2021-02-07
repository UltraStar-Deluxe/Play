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

public class NoteAreaSelectionDragListener : MonoBehaviour, INeedInjection, IDragListener<NoteAreaDragEvent>
{
    [InjectedInInspector]
    public RectTransform selectionFrame;

    [InjectedInInspector]
    public RectTransform noteContainer;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    [Inject]
    private Canvas canvas;

    bool isCanceled;

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
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
        UpdateSelectionFrame(dragEvent);
        UpdateSelection(dragEvent);
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        selectionFrame.gameObject.SetActive(false);
    }

    public void CancelDrag()
    {
        selectionFrame.gameObject.SetActive(false);
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void UpdateSelection(NoteAreaDragEvent dragEvent)
    {
        List<EditorUiNote> selectedUiNotes = new List<EditorUiNote>();
        foreach (Transform child in noteContainer.transform)
        {
            EditorUiNote uiNote = child.GetComponent<EditorUiNote>();
            if (uiNote != null && IsInSelectionFrame(uiNote, dragEvent))
            {
                selectedUiNotes.Add(uiNote);
            }
        }

        // Add to selection via Shift. Remove from selection via Ctrl+Shift. Without modifier, set selection.
        if (InputUtils.IsKeyboardShiftPressed())
        {
            if (InputUtils.IsKeyboardControlPressed())
            {
                selectionController.RemoveFromSelection(selectedUiNotes);
            }
            else
            {
                selectionController.AddToSelection(selectedUiNotes);
            }
        }
        else
        {
            selectionController.SetSelection(selectedUiNotes);
        }
    }

    private bool IsInSelectionFrame(EditorUiNote uiNote, NoteAreaDragEvent dragEvent)
    {
        Note note = uiNote.Note;
        if (note == null)
        {
            return false;
        }

        int startMidiNote = dragEvent.MidiNoteDragStart;
        int endMidiNote = dragEvent.MidiNoteDragStart + dragEvent.MidiNoteDistance;
        if (startMidiNote > endMidiNote)
        {
            ObjectUtils.Swap(ref startMidiNote, ref endMidiNote);
        }

        int startBeat = dragEvent.PositionInSongInBeatsDragStart;
        int endBeat = dragEvent.PositionInSongInBeatsDragStart + dragEvent.BeatDistance;
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
        float x = dragEvent.GeneralDragEvent.StartPositionInPixels.x;
        float y = dragEvent.GeneralDragEvent.StartPositionInPixels.y;
        float width = dragEvent.GeneralDragEvent.DistanceInPixels.x / canvasScale.x;
        float height = -dragEvent.GeneralDragEvent.DistanceInPixels.y / canvasScale.y;

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
}
