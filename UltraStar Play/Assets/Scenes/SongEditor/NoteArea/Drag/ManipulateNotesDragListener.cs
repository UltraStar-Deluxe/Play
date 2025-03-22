using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ManipulateNotesDragListener : INeedInjection, IInjectionFinishedListener, IDragListener<NoteAreaDragEvent>
{
    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private PanelHelper panelHelper;

    private List<Note> selectedNotes = new();
    private List<Note> followingNotes = new();

    private readonly Dictionary<Note, Note> noteToSnapshotOfNoteMap = new();
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

    public void OnInjectionFinished()
    {
        noteAreaControl.DragControl.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        if (dragEvent.GeneralDragEvent.InputButton != (int)PointerEventData.InputButton.Left)
        {
            AbortDrag();
            return;
        }

        Vector2 startPosInPanelCoordinates = dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition;
        VisualElement pickedElement = uiDocument.rootVisualElement.panel.Pick(startPosInPanelCoordinates);
        if (pickedElement.userData is not EditorNoteControl dragStartNoteControl
            || !dragStartNoteControl.Note.IsEditable)
        {
            return;
        }

        isCanceled = false;
        if (!selectionControl.IsSelected(dragStartNoteControl.Note))
        {
            selectionControl.SetSelection(new List<EditorNoteControl> { dragStartNoteControl });
        }

        selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            AbortDrag();
            return;
        }

        dragAction = GetDragAction(dragStartNoteControl, dragEvent);

        if (settings.SongEditorSettings.AdjustFollowingNotes)
        {
            followingNotes = SongEditorSongMetaUtils.GetFollowingNotes(songMeta, selectedNotes);
        }
        else
        {
            followingNotes.Clear();
        }

        CreateSnapshot(selectedNotes.Union(followingNotes));
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        if (selectedNotes.Count <= 0)
        {
            return;
        }

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
                if (InputUtils.IsKeyboardShiftPressed()
                    || selectedNotes.Count == 1)
                {
                    ExtendNotesLeft(dragEvent, selectedNotes);
                }
                else
                {
                    StretchNotesLeft(dragEvent, selectedNotes);
                }
                break;

            case DragAction.StretchRight:
                if (InputUtils.IsKeyboardShiftPressed()
                    || selectedNotes.Count == 1)
                {
                    ExtendNotesRight(dragEvent, selectedNotes, true);
                }
                else
                {
                    StretchNotesRight(dragEvent, selectedNotes, true);
                }
                break;
            default:
                throw new UnityException("Unknown drag action: " + dragAction);
        }

        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        if (noteToSnapshotOfNoteMap.Count > 0)
        {
            // Values have been directly applied to the notes. The snapshot can be cleared.
            noteToSnapshotOfNoteMap.Clear();
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }
    }

    private void AbortDrag()
    {
        isCanceled = true;
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

    private DragAction GetDragAction(EditorNoteControl dragStartNoteControl, NoteAreaDragEvent dragEvent)
    {
        if (dragStartNoteControl.IsPositionOverLeftHandle(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition))
        {
            return DragAction.StretchLeft;
        }
        else if (dragStartNoteControl.IsPositionOverRightHandle(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition))
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
            if (noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                int newMidiNote = noteSnapshot.MidiNote - dragEvent.MidiNoteDistance;
                note.SetMidiNote(newMidiNote);
                note.SetStartAndEndBeat(noteSnapshot.StartBeat, noteSnapshot.EndBeat);
            }
        }

        if (SongEditorSettingsUtils.ShouldAdjustFollowingNotes(settings, adjustFollowingNotesIfNeeded))
        {
            MoveNotesVertical(dragEvent, followingNotes, false);
        }
    }

    private void MoveNotesHorizontal(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        foreach (Note note in notes)
        {
            if (noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
                int newEndBeat = noteSnapshot.EndBeat + dragEvent.BeatDistance;
                note.SetMidiNote(noteSnapshot.MidiNote);
                note.SetStartAndEndBeat(newStartBeat, newEndBeat);
            }
        }

        if (SongEditorSettingsUtils.ShouldAdjustFollowingNotes(settings, adjustFollowingNotesIfNeeded))
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }
    }

    private void ExtendNotesRight(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        foreach (Note note in notes)
        {
            if (noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                int newEndBeat = noteSnapshot.EndBeat + dragEvent.BeatDistance;
                if (newEndBeat > noteSnapshot.StartBeat)
                {
                    note.SetEndBeat(newEndBeat);
                }
            }
        }

        if (SongEditorSettingsUtils.ShouldAdjustFollowingNotes(settings, adjustFollowingNotesIfNeeded))
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }
    }

    private void ExtendNotesLeft(NoteAreaDragEvent dragEvent, List<Note> notes)
    {
        foreach (Note note in notes)
        {
            if (noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                // Extend/trim StartBeat
                int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
                if (newStartBeat < noteSnapshot.EndBeat)
                {
                    note.SetStartBeat(newStartBeat);
                }
            }
        }
    }

    private void StretchNotesLeft(NoteAreaDragEvent dragEvent, List<Note> notes)
    {
        List<Note> snapshotNotes = notes.Select(note => noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot) ? noteSnapshot : null)
            .Where(note => note != null)
            .ToList();
        if (snapshotNotes.IsNullOrEmpty())
        {
            return;
        }

        int minBeatInSelection = snapshotNotes.Select(note => note.StartBeat).Min();
        int maxBeatInSelection = snapshotNotes.Select(note => note.EndBeat).Max();
        int anchorBeatInSelection = maxBeatInSelection;
        int currentPointerBeat = dragEvent.PositionInBeatsDragStart + dragEvent.BeatDistance;
        float dragPercentRelativeToSelection = (float)(dragEvent.PositionInBeatsDragStart - currentPointerBeat)
                                               / (maxBeatInSelection - minBeatInSelection);

        foreach (Note note in notes)
        {
            if (!noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                continue;
            }

            // Stretch/shrink StartBeat and EndBeat relative to selection
            float newStartBeat = anchorBeatInSelection + (noteSnapshot.StartBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            float newEndBeat = anchorBeatInSelection + (noteSnapshot.EndBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            note.SetStartAndEndBeat((int)newStartBeat, (int)newEndBeat);
        }
    }

    private void StretchNotesRight(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        List<Note> snapshotNotes = notes.Select(note => noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot) ? noteSnapshot : null)
            .Where(note => note != null)
            .ToList();
        if (snapshotNotes.IsNullOrEmpty())
        {
            return;
        }

        int minBeatInSelection = snapshotNotes.Select(note => note.StartBeat).Min();
        int maxBeatInSelection = snapshotNotes.Select(note => note.EndBeat).Max();
        int anchorBeatInSelection = minBeatInSelection;
        int currentPointerBeat = dragEvent.PositionInBeatsDragStart + dragEvent.BeatDistance;
        float dragPercentRelativeToSelection = (float)(currentPointerBeat - dragEvent.PositionInBeatsDragStart)
                                               / (maxBeatInSelection - minBeatInSelection);

        foreach (Note note in notes)
        {
            if (!noteToSnapshotOfNoteMap.TryGetValue(note, out Note noteSnapshot))
            {
                continue;
            }

            // Stretch/shrink StartBeat and EndBeat relative to selection
            float newStartBeat = anchorBeatInSelection + (noteSnapshot.StartBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            float newEndBeat = anchorBeatInSelection + (noteSnapshot.EndBeat - anchorBeatInSelection) * (1 + dragPercentRelativeToSelection);
            note.SetStartAndEndBeat((int)newStartBeat, (int)newEndBeat);
        }

        if (SongEditorSettingsUtils.ShouldAdjustFollowingNotes(settings, adjustFollowingNotesIfNeeded))
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }
    }
}
