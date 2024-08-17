using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ManipulateSentenceDragListener : INeedInjection, IInjectionFinishedListener, IDragListener<NoteAreaDragEvent>
{
    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private EditorSentenceControl sentenceControl;

    [Inject]
    private NoteAreaControl noteAreaControl;

    private List<Note> selectedNotes = new();
    private List<Note> followingNotes = new();

    private readonly Dictionary<Note, Note> noteToSnapshotOfNoteMap = new();
    private int linebreakBeatSnapshot;
    private bool isCanceled;

    private DragAction dragAction;
    private enum DragAction
    {
        Move,
        StretchRight
    }

    public void OnInjectionFinished()
    {
        noteAreaControl.DragControl.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        if (dragEvent.GeneralDragEvent.InputButton != (int)PointerEventData.InputButton.Left
            || !sentenceControl.IsPointerOver)
        {
            AbortDrag();
            return;
        }

        isCanceled = false;
        dragAction = GetDragAction(sentenceControl, dragEvent);

        selectedNotes = sentenceControl.Sentence.Notes.ToList();
        if (selectedNotes.IsNullOrEmpty())
        {
            AbortDrag();
            return;
        }

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
                MoveNotesHorizontal(dragEvent, selectedNotes, true);
                break;

            case DragAction.StretchRight:
                ChangeLinebreakBeat(dragEvent);
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
            songMetaChangeEventStream.OnNext(new NotesChangedEvent());
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
        linebreakBeatSnapshot = sentenceControl.Sentence.LinebreakBeat;
    }

    private DragAction GetDragAction(EditorSentenceControl dragStartSentenceControl, NoteAreaDragEvent dragEvent)
    {
        if (dragStartSentenceControl.IsPositionOverRightHandle(dragEvent.GeneralDragEvent.ScreenCoordinateInPixels.StartPosition))
        {
            return DragAction.StretchRight;
        }
        return DragAction.Move;
    }

    private void MoveNotesHorizontal(NoteAreaDragEvent dragEvent, List<Note> notes, bool adjustFollowingNotesIfNeeded)
    {
        if (notes.IsNullOrEmpty()
            || dragEvent.BeatDistance == 0)
        {
            return;
        }

        foreach (Note note in notes)
        {
            Note noteSnapshot = noteToSnapshotOfNoteMap[note];
            int newStartBeat = noteSnapshot.StartBeat + dragEvent.BeatDistance;
            int newEndBeat = noteSnapshot.EndBeat + dragEvent.BeatDistance;
            note.SetStartAndEndBeat(newStartBeat, newEndBeat);
        }

        if (SongEditorSettingsUtils.ShouldAdjustFollowingNotes(settings, adjustFollowingNotesIfNeeded))
        {
            MoveNotesHorizontal(dragEvent, followingNotes, false);
        }

        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }

    private void ChangeLinebreakBeat(NoteAreaDragEvent dragEvent)
    {
        if (dragEvent.BeatDistance == 0)
        {
            return;
        }

        sentenceControl.Sentence.SetLinebreakBeat(linebreakBeatSnapshot + dragEvent.BeatDistance);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }

    public void Dispose()
    {
        noteAreaControl.DragControl.RemoveListener(this);
    }
}
