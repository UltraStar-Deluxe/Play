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

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ManipulateNotesDragListener : MonoBehaviour, INeedInjection, IDragListener<NoteAreaDragEvent>
{
    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    private List<Note> selectedNotes = new List<Note>();
    private List<Note> followingNotes = new List<Note>();

    private readonly Dictionary<Note, Note> noteToSnapshotOfNoteMap = new Dictionary<Note, Note>();
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
        if (dragEvent.GeneralDragEvent.InputButton != (int)PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        isCanceled = false;
        GameObject raycastTarget = dragEvent.GeneralDragEvent.RaycastResultsDragStart.Select(it => it.gameObject).FirstOrDefault();
        EditorUiNote dragStartUiNote = raycastTarget.GetComponent<EditorUiNote>();
        if (dragStartUiNote == null)
        {
            CancelDrag();
            return;
        }

        if (!selectionControl.IsSelected(dragStartUiNote.Note))
        {
            selectionControl.SetSelection(new List<EditorUiNote> { dragStartUiNote });
        }

        dragAction = GetDragAction(dragStartUiNote, dragEvent);

        selectedNotes = selectionControl.GetSelectedNotes();
        if (settings.SongEditorSettings.AdjustFollowingNotes)
        {
            followingNotes = SongMetaUtils.GetFollowingNotes(songMeta, selectedNotes);
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
                if (InputUtils.IsKeyboardShiftPressed() && selectedNotes.Count > 1)
                {
                    StretchNotesLeft(dragEvent, selectedNotes);
                }
                else
                {
                    ExtendNotesLeft(dragEvent, selectedNotes);
                }
                break;

            case DragAction.StretchRight:
                if (InputUtils.IsKeyboardShiftPressed() && selectedNotes.Count > 1)
                {
                    StretchNotesRight(dragEvent, selectedNotes, true);
                }
                else
                {
                    ExtendNotesRight(dragEvent, selectedNotes, true);
                }
                break;
            default:
                throw new UnityException("Unkown drag action: " + dragAction);
        }

        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        if (noteToSnapshotOfNoteMap.Count > 0)
        {
            // Values have been directly applied to the notes. The snapshot can be cleared.
            noteToSnapshotOfNoteMap.Clear();
            songMetaChangeEventStream.OnNext(new NotesChangedEvent());
        }
    }

    public void CancelDrag()
    {
        isCanceled = true;
        foreach (KeyValuePair<Note, Note> noteAndSnapshotOfNote in noteToSnapshotOfNoteMap)
        {
            Note note = noteAndSnapshotOfNote.Key;
            Note snapshotOfNote = noteAndSnapshotOfNote.Value;
            note.CopyValues(snapshotOfNote);
        }
        noteToSnapshotOfNoteMap.Clear();

        editorNoteDisplayer.UpdateNotesAndSentences();
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
        if (dragStartUiNote.IsPositionOverLeftHandle(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition))
        {
            return DragAction.StretchLeft;
        }
        else if (dragStartUiNote.IsPositionOverRightHandle(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition))
        {
            return DragAction.StretchRight;
        }
        return DragAction.Move;
    }

    private DragDirection GetDragDirection(NoteAreaDragEvent dragEvent)
    {
        if (Math.Abs(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.Distance.y) > Math.Abs(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.Distance.x))
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

    private void ExtendNotesRight(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
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

    private void ExtendNotesLeft(NoteAreaDragEvent dragEvent, List<Note> notes)
    {
        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            // Extend/trim StartBeat
            int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
            if (newStartBeat < noteSnapshot.EndBeat)
            {
                note.SetStartBeat(newStartBeat);
            }
        }
    }

    private void StretchNotesLeft(NoteAreaDragEvent dragEvent, List<Note> notes)
    {
        List<Note> snapshotNotes = notes.Select(note => noteToSnapshotOfNoteMap[note]).ToList();
        int minBeatInSelection = snapshotNotes.Select(note => note.StartBeat).Min();
        int maxBeatInSelection = snapshotNotes.Select(note => note.EndBeat).Max();
        int anchorBeatInSelection = maxBeatInSelection;
        int currentPointerBeat = dragEvent.PositionInSongInBeatsDragStart + dragEvent.BeatDistance;
        float dragPercentRelativeToSelection = (float)(dragEvent.PositionInSongInBeatsDragStart - currentPointerBeat)
                                               / (maxBeatInSelection - minBeatInSelection);

        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            // Stretch/shrink StartBeat and EndBeat relative to selection
            float newStartBeat = anchorBeatInSelection + (noteSnapshot.StartBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            float newEndBeat = anchorBeatInSelection + (noteSnapshot.EndBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            note.SetStartAndEndBeat((int)newStartBeat, (int)newEndBeat);
        }
    }

    private void StretchNotesRight(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        List<Note> snapshotNotes = notes.Select(note => noteToSnapshotOfNoteMap[note]).ToList();
        int minBeatInSelection = snapshotNotes.Select(note => note.StartBeat).Min();
        int maxBeatInSelection = snapshotNotes.Select(note => note.EndBeat).Max();
        int anchorBeatInSelection = minBeatInSelection;
        int currentPointerBeat = dragEvent.PositionInSongInBeatsDragStart + dragEvent.BeatDistance;
        float dragPercentRelativeToSelection = (float)(currentPointerBeat - dragEvent.PositionInSongInBeatsDragStart)
                                               / (maxBeatInSelection - minBeatInSelection);

        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            // Stretch/shrink StartBeat and EndBeat relative to selection
            float newStartBeat = anchorBeatInSelection + (noteSnapshot.StartBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            float newEndBeat = anchorBeatInSelection + (noteSnapshot.EndBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            note.SetStartAndEndBeat((int)newStartBeat, (int)newEndBeat);
        }

        if (adjustFollowingNotesIfNeeded && settings.SongEditorSettings.AdjustFollowingNotes)
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }
    }
}
