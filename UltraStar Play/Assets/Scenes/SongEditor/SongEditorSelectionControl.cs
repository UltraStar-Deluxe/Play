﻿using System;
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

public class SongEditorSelectionControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private EventSystem eventSystem;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorLayerManager layerManager;
    
    private readonly NoteHashSet selectedNotes = new NoteHashSet();

    public List<Note> GetSelectedNotes()
    {
        return new List<Note>(selectedNotes);
    }

    public bool HasSelectedNotes()
    {
        return selectedNotes != null
               && selectedNotes.Count > 0;
    }
    
    public bool IsSelected(Note note)
    {
        return selectedNotes.Contains(note);
    }

    public void ClearSelection()
    {
        foreach (EditorNoteControl uiNote in editorNoteDisplayer.EditorNoteControls)
        {
            uiNote.SetSelected(false);
        }
        selectedNotes.Clear();
    }

    public void SelectAll()
    {
        List<Note> allNotes = songEditorSceneControl.GetAllVisibleNotes();
        SetSelection(allNotes);
    }

    public void AddToSelection(List<EditorNoteControl> uiNotes)
    {
        foreach (EditorNoteControl uiNote in uiNotes)
        {
            AddToSelection(uiNote);
        }
    }

    public void RemoveFromSelection(List<EditorNoteControl> uiNotes)
    {
        foreach (EditorNoteControl uiNote in uiNotes)
        {
            RemoveFromSelection(uiNote);
        }
    }

    public void SetSelection(List<EditorNoteControl> uiNotes)
    {
        ClearSelection();
        foreach (EditorNoteControl uiNote in uiNotes)
        {
            AddToSelection(uiNote);
        }
    }

    public void SetSelection(List<Note> notes)
    {
        ClearSelection();
        foreach (Note note in notes)
        {
            if (!layerManager.IsVisible(note))
            {
                continue;
            }
            selectedNotes.Add(note);

            EditorNoteControl noteControl = editorNoteDisplayer.GetNoteControl(note);
            if (noteControl != null)
            {
                noteControl.SetSelected(true);
            }
        }
    }

    public void AddToSelection(List<Note> notes)
    {
        notes.ForEach(AddToSelection);
    }

    public void AddToSelection(Note note)
    {
        selectedNotes.Add(note);
        EditorNoteControl noteControl = editorNoteDisplayer.GetNoteControl(note);
        if (noteControl != null)
        {
            noteControl.SetSelected(true);
        }
    }

    public void AddToSelection(EditorNoteControl noteControl)
    {
        noteControl.SetSelected(true);
        selectedNotes.Add(noteControl.Note);
    }

    public void RemoveFromSelection(List<Note> notes)
    {
        notes.ForEach(RemoveFromSelection);
    }

    public void RemoveFromSelection(Note note)
    {
        selectedNotes.Remove(note);
        EditorNoteControl noteControl = editorNoteDisplayer.GetNoteControl(note);
        if (noteControl != null)
        {
            noteControl.SetSelected(false);
        }
    }

    public void RemoveFromSelection(EditorNoteControl noteControl)
    {
        noteControl.SetSelected(false);
        selectedNotes.Remove(noteControl.Note);
    }

    public void SelectNextNote(bool updatePositionInSong = true)
    {
        bool wasEditingLyrics = false;
        if (GameObjectUtils.InputFieldHasFocus(eventSystem))
        {
            // Finish this lyrics editing and select following note directly in lyrics editing mode.
            GameObject selectedGameObject = eventSystem.currentSelectedGameObject;
            EditorNoteLyricsInputField lyricsInputField = selectedGameObject.GetComponentInChildren<EditorNoteLyricsInputField>();
            if (lyricsInputField != null)
            {
                wasEditingLyrics = true;
                SetSelection(new List<EditorNoteControl> { lyricsInputField.EditorNoteControl });
            }
        }

        if (selectedNotes.Count == 0)
        {
            SelectFirstVisibleNote();
            return;
        }

        List<Note> notes = songEditorSceneControl.GetAllVisibleNotes();
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

            if (wasEditingLyrics)
            {
                songEditorSceneControl.StartEditingNoteText();
                // When the newly selected note has not been drawn yet (because it is not in the current viewport),
                // then the lyric edit mode might not have been started. To fix this, open lyrics edit mode again 1 frame later.
                StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
                    () => songEditorSceneControl.StartEditingNoteText()));
            }
        }
    }

    public void SelectPreviousNote(bool updatePositionInSong = true)
    {
        bool wasEditingLyrics = false;
        if (GameObjectUtils.InputFieldHasFocus(eventSystem))
        {
            // Finish this lyrics editing and select following note directly in lyrics editing mode.
            GameObject selectedGameObject = eventSystem.currentSelectedGameObject;
            EditorNoteLyricsInputField lyricsInputField = selectedGameObject.GetComponentInChildren<EditorNoteLyricsInputField>();
            if (lyricsInputField != null)
            {
                wasEditingLyrics = true;
                SetSelection(new List<EditorNoteControl> { lyricsInputField.EditorNoteControl });
            }
        }

        if (selectedNotes.Count == 0)
        {
            SelectLastVisibleNote();
            return;
        }

        List<Note> notes = songEditorSceneControl.GetAllVisibleNotes();
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

            if (wasEditingLyrics)
            {
                songEditorSceneControl.StartEditingNoteText();
                // When the newly selected note has not been drawn yet (because it is not in the current viewport),
                // then the lyric edit mode might not have been started. To fix this, open lyrics edit mode again 1 frame later.
                StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
                    () => songEditorSceneControl.StartEditingNoteText()));
            }
        }
    }

    private void SelectFirstVisibleNote()
    {
        List<EditorNoteControl> sortedUiNotes = GetSortedVisibleUiNotes();
        if (sortedUiNotes.IsNullOrEmpty())
        {
            return;
        }

        SetSelection(new List<EditorNoteControl> { sortedUiNotes.First() });
    }

    private void SelectLastVisibleNote()
    {
        List<EditorNoteControl> sortedUiNotes = GetSortedVisibleUiNotes();
        if (sortedUiNotes.IsNullOrEmpty())
        {
            return;
        }

        SetSelection(new List<EditorNoteControl> { sortedUiNotes.Last() });
    }

    private List<EditorNoteControl> GetSortedVisibleUiNotes()
    {
        if (editorNoteDisplayer.EditorNoteControls.IsNullOrEmpty())
        {
            return new List<EditorNoteControl>();
        }

        List<EditorNoteControl> sortedUiNote = new List<EditorNoteControl>(editorNoteDisplayer.EditorNoteControls);
        sortedUiNote.Sort(EditorNoteControl.comparerByStartBeat);
        return sortedUiNote;
    }
}