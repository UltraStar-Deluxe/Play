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
        EditorUiNote[] selectedUiNotes = uiNoteContainer.GetComponentsInChildren<EditorUiNote>();
        foreach (EditorUiNote uiNote in selectedUiNotes)
        {
            uiNote.SetSelected(false);
        }
        selectedNotes.Clear();
    }

    public void SetSelection(List<EditorUiNote> uiNotes)
    {
        ClearSelection();
        foreach (EditorUiNote uiNote in uiNotes)
        {
            AddToSelection(uiNote);
        }
    }

    private void AddToSelection(EditorUiNote uiNote)
    {
        uiNote.SetSelected(true);
        selectedNotes.Add(uiNote.Note);
    }
}
