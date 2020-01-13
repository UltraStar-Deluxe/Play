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

public class NoteAreaSelectionDragListener : MonoBehaviour, INeedInjection, INoteAreaDragListener
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

    bool isCanceled;

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
        if (dragEvent.InputButton != PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        GameObject raycastTarget = dragEvent.RaycastResultsDragStart.Select(it => it.gameObject).FirstOrDefault();
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
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKey(KeyCode.LeftControl))
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
        float x = dragEvent.XDragStartInPixels;
        float y = dragEvent.YDragStartInPixels;
        float width = dragEvent.XDistanceInPixels;
        float height = -dragEvent.YDistanceInPixels;

        if (width < 0)
        {
            width = -width;
            x -= width;
        }
        if (height < 0)
        {
            height = -height;
            y += height;
        }
        selectionFrame.position = new Vector2(x, y);
        selectionFrame.sizeDelta = new Vector2(width, height);
    }
}
