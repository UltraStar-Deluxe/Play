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

public class SongEditorSelectionController : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public RectTransform uiNoteContainer;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    private readonly NoteHashSet selectedNotes = new NoteHashSet();

    public List<Note> GetSelectedNotes()
    {
        return new List<Note>(selectedNotes);
    }

    public bool IsSelected(Note note)
    {
        return selectedNotes.Contains(note);
    }

    public void ClearSelection()
    {
        EditorUiNote[] uiNotes = uiNoteContainer.GetComponentsInChildren<EditorUiNote>();
        foreach (EditorUiNote uiNote in uiNotes)
        {
            uiNote.SetSelected(false);
        }
        selectedNotes.Clear();
    }

    public void SelectAll()
    {
        List<Note> allNotes = songEditorSceneController.GetAllNotes();
        SetSelection(allNotes);
    }

    public void AddToSelection(List<EditorUiNote> uiNotes)
    {
        foreach (EditorUiNote uiNote in uiNotes)
        {
            AddToSelection(uiNote);
        }
    }

    public void RemoveFromSelection(List<EditorUiNote> uiNotes)
    {
        foreach (EditorUiNote uiNote in uiNotes)
        {
            RemoveFromSelection(uiNote);
        }
    }

    public void SetSelection(List<EditorUiNote> uiNotes)
    {
        ClearSelection();
        foreach (EditorUiNote uiNote in uiNotes)
        {
            AddToSelection(uiNote);
        }
    }

    public void SetSelection(List<Note> notes)
    {
        ClearSelection();
        foreach (Note note in notes)
        {
            selectedNotes.Add(note);

            EditorUiNote uiNote = editorNoteDisplayer.GetUiNoteForNote(note);
            if (uiNote != null)
            {
                uiNote.SetSelected(true);
            }
        }
    }

    public void AddToSelection(Note note)
    {
        selectedNotes.Add(note);
        EditorUiNote uiNote = editorNoteDisplayer.GetUiNoteForNote(note);
        if (uiNote != null)
        {
            uiNote.SetSelected(true);
        }
    }

    public void AddToSelection(EditorUiNote uiNote)
    {
        uiNote.SetSelected(true);
        selectedNotes.Add(uiNote.Note);
    }

    public void RemoveFromSelection(Note note)
    {
        selectedNotes.Remove(note);
        EditorUiNote uiNote = editorNoteDisplayer.GetUiNoteForNote(note);
        if (uiNote != null)
        {
            uiNote.SetSelected(false);
        }
    }

    public void RemoveFromSelection(EditorUiNote uiNote)
    {
        uiNote.SetSelected(false);
        selectedNotes.Remove(uiNote.Note);
    }

    public void SelectNextNote(bool updatePositionInSong = true)
    {
        if (selectedNotes.Count == 0)
        {
            SelectFirstVisibleNote();
            return;
        }

        List<Note> notes = songEditorSceneController.GetAllNotes();
        int maxEndBeat = selectedNotes.Select(it => it.EndBeat).Max();

        // Find the next note, i.e., the note right of maxEndBeat with the smallest distance to it.
        int smallestDistance = int.MaxValue;
        Note nextNote = null;
        foreach (Note note in notes)
        {
            if (note.StartBeat >= maxEndBeat)
            {
                int distance = note.StartBeat - maxEndBeat;
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    nextNote = note;
                }
            }
        }

        if (nextNote != null)
        {
            SetSelection(new List<Note> { nextNote });

            if (updatePositionInSong)
            {
                double noteStartInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, nextNote.StartBeat);
                songAudioPlayer.PositionInSongInMillis = noteStartInMillis;
            }
        }
    }

    public void SelectPreviousNote(bool updatePositionInSong = true)
    {
        if (selectedNotes.Count == 0)
        {
            SelectLastVisibleNote();
            return;
        }

        List<Note> notes = songEditorSceneController.GetAllNotes();
        int minStartBeat = selectedNotes.Select(it => it.StartBeat).Min();

        // Find the previous note, i.e., the note left of minStartBeat with the smallest distance to it.
        int smallestDistance = int.MaxValue;
        Note previousNote = null;
        foreach (Note note in notes)
        {
            if (minStartBeat >= note.EndBeat)
            {
                int distance = minStartBeat - note.EndBeat;
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    previousNote = note;
                }
            }
        }

        if (previousNote != null)
        {
            SetSelection(new List<Note> { previousNote });

            if (updatePositionInSong)
            {
                double noteStartInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, previousNote.StartBeat);
                songAudioPlayer.PositionInSongInMillis = noteStartInMillis;
            }
        }
    }

    private void SelectFirstVisibleNote()
    {
        List<EditorUiNote> sortedUiNotes = GetSortedVisibleUiNotes();
        if (sortedUiNotes.IsNullOrEmpty())
        {
            return;
        }

        SetSelection(new List<EditorUiNote> { sortedUiNotes.First() });
    }

    private void SelectLastVisibleNote()
    {
        List<EditorUiNote> sortedUiNotes = GetSortedVisibleUiNotes();
        if (sortedUiNotes.IsNullOrEmpty())
        {
            return;
        }

        SetSelection(new List<EditorUiNote> { sortedUiNotes.Last() });
    }

    private List<EditorUiNote> GetSortedVisibleUiNotes()
    {
        EditorUiNote[] uiNotes = uiNoteContainer.GetComponentsInChildren<EditorUiNote>();
        if (uiNotes.IsNullOrEmpty())
        {
            return new List<EditorUiNote>();
        }

        List<EditorUiNote> sortedUiNote = new List<EditorUiNote>(uiNotes);
        sortedUiNote.Sort(EditorUiNote.comparerByStartBeat);
        return sortedUiNote;
    }
}
