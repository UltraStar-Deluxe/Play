using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.Text;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    SongEditorSceneController songEditorSceneController;

    [Inject]
    SongEditorSelectionController selectionController;

    [Inject]
    SongEditorLayerManager layerManager;

    private EditorUiNote uiNote;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        if (uiNote == null)
        {
            uiNote = GetComponent<EditorUiNote>();
        }
        if (!selectionController.IsSelected(uiNote.Note))
        {
            selectionController.SetSelection(new List<EditorUiNote> { uiNote });
        }

        contextMenu.AddItem("Split Notes", () => OnSplitNotes());
        contextMenu.AddItem("Merge Notes", () => OnMergeNotes());
    }

    private void OnSplitNotes()
    {
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        foreach (Note note in selectedNotes)
        {
            if (note.Length > 1)
            {
                int splitBeat = note.StartBeat + (note.Length / 2);
                Note newNote = new Note(note.Type, splitBeat, note.EndBeat - splitBeat, note.TxtPitch, "~");
                newNote.SetSentence(note.Sentence);
                note.SetEndBeat(splitBeat);
            }
        }
        songEditorSceneController.OnNotesChanged();
    }

    private void OnMergeNotes()
    {
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        selectedNotes.Sort(Note.comparerByStartBeat);
        int minBeat = selectedNotes[0].StartBeat;
        int maxBeat = selectedNotes.Select(it => it.EndBeat).Max();
        StringBuilder stringBuilder = new StringBuilder();
        foreach (Note note in selectedNotes)
        {
            if (stringBuilder.Length == 0 || note.Text != "~")
            {
                stringBuilder.Append(note.Text);
            }
        }
        Note targetNote = uiNote.Note;
        Note mergedNote = new Note(targetNote.Type, minBeat, maxBeat - minBeat, targetNote.TxtPitch, stringBuilder.ToString());
        mergedNote.SetSentence(targetNote.Sentence);

        // Remove old notes
        foreach (Note note in selectedNotes)
        {
            songEditorSceneController.DeleteNote(note);
        }
        songEditorSceneController.OnNotesChanged();
    }
}
