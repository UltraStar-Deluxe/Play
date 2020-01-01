using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ManipulateNotesDragListener : MonoBehaviour, INeedInjection, INoteAreaDragListener
{
    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private Settings settings;

    private List<Note> selectedNotes;
    private List<Note> followingNotes;

    private Dictionary<Note, Note> noteToSnapshotOfNoteMap = new Dictionary<Note, Note>();
    private bool isCanceled;

    private DragAction dragAction;
    private enum DragAction
    {
        Move,
        StretchLeft,
        StretchRight
    }

    private enum DragDirection
    {
        Horizontal,
        Vertical
    }

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
        GameObject raycastTarget = dragEvent.RaycastResultsDragStart.Select(it => it.gameObject).FirstOrDefault();
        EditorUiNote dragStartUiNote = raycastTarget.GetComponent<EditorUiNote>();
        if (dragStartUiNote == null)
        {
            CancelDrag();
            return;
        }

        if (!selectionController.IsSelected(dragStartUiNote.Note))
        {
            selectionController.SetSelection(new List<EditorUiNote> { dragStartUiNote });
        }

        dragAction = GetDragAction(dragStartUiNote, dragEvent);

        selectedNotes = selectionController.GetSelectedNotes();
        if (settings.SongEditorSettings.AdjustFollowingNotes)
        {
            followingNotes = songEditorSceneController.GetFollowingNotes(selectedNotes);
        }
        else
        {
            followingNotes.Clear();
        }

        CreateSnapshot(selectedNotes.Union(followingNotes));
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        switch (dragAction)
        {
            case DragAction.Move:
                DragDirection dragDirection = GetDragDirection(dragEvent);
                if (dragDirection == DragDirection.Horizontal)
                {
                    MoveNotesHorizontal(dragEvent, selectedNotes, true);
                }
                else
                {
                    MoveNotesVertical(dragEvent, selectedNotes, true);
                }
                break;

            case DragAction.StretchLeft:
                StretchNotesLeft(dragEvent, selectedNotes);
                break;

            case DragAction.StretchRight:
                StretchNotesRight(dragEvent, selectedNotes, true);
                break;
        }
        songEditorSceneController.OnNotesChanged();
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        // Values have been directly applied to the notes. The snapshot can be cleared.
        noteToSnapshotOfNoteMap.Clear();
    }

    public void CancelDrag()
    {
        isCanceled = true;
        // Reset notes to snapshot
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        foreach (Note note in selectedNotes)
        {
            if (noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                note.CopyValues(noteSnapshot);
            }
        }
        noteToSnapshotOfNoteMap.Clear();
        songEditorSceneController.OnNotesChanged();
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void CreateSnapshot(IEnumerable<Note> notes)
    {
        noteToSnapshotOfNoteMap.Clear();
        foreach (Note note in notes)
        {
            Note noteClone = note.Clone();
            noteToSnapshotOfNoteMap.Add(note, noteClone);
        }
    }

    private DragAction GetDragAction(EditorUiNote dragStartUiNote, NoteAreaDragEvent dragEvent)
    {
        if (dragStartUiNote.IsPositionOverLeftHandle(dragEvent.DragStartPositionInPixels))
        {
            return DragAction.StretchLeft;
        }
        else if (dragStartUiNote.IsPositionOverRightHandle(dragEvent.DragStartPositionInPixels))
        {
            return DragAction.StretchRight;
        }
        return DragAction.Move;
    }

    private DragDirection GetDragDirection(NoteAreaDragEvent dragEvent)
    {
        if (Math.Abs(dragEvent.YDistanceInPixels) > Math.Abs(dragEvent.XDistanceInPixels))
        {
            return DragDirection.Vertical;
        }
        return DragDirection.Horizontal;
    }

    private void MoveNotesVertical(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            int newMidiNote = noteSnapshot.MidiNote + dragEvent.MidiNoteDistance;
            note.SetMidiNote(newMidiNote);
            note.SetStartAndEndBeat(noteSnapshot.StartBeat, noteSnapshot.EndBeat);
        }

        if (adjustFollowingNotesIfNeeded && settings.SongEditorSettings.AdjustFollowingNotes)
        {
            MoveNotesVertical(dragEvent, followingNotes, false);
        }
    }

    private void MoveNotesHorizontal(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
            int newEndBeat = noteSnapshot.EndBeat + dragEvent.BeatDistance;
            note.SetMidiNote(noteSnapshot.MidiNote);
            note.SetStartAndEndBeat(newStartBeat, newEndBeat);
        }

        if (adjustFollowingNotesIfNeeded && settings.SongEditorSettings.AdjustFollowingNotes)
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }
    }

    private void StretchNotesRight(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            int newEndBeat = noteSnapshot.EndBeat + dragEvent.BeatDistance;
            if (newEndBeat > noteSnapshot.StartBeat)
            {
                note.SetEndBeat(newEndBeat);
            }
        }

        if (adjustFollowingNotesIfNeeded && settings.SongEditorSettings.AdjustFollowingNotes)
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }
    }

    private void StretchNotesLeft(NoteAreaDragEvent dragEvent, List<Note> notes)
    {
        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
            if (newStartBeat < noteSnapshot.EndBeat)
            {
                note.SetStartBeat(newStartBeat);
            }
        }
    }
}
