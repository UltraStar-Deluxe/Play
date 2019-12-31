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

        if (CanMoveToPreviousSentence())
        {
            contextMenu.AddSeparator();
            contextMenu.AddItem("Move to previous line", () => OnMoveToPreviousSentence());
        }
        if (CanMoveToNextSentence())
        {
            contextMenu.AddSeparator();
            contextMenu.AddItem("Move to next line", () => OnMoveToNextSentence());
        }

        contextMenu.AddSeparator();
        contextMenu.AddItem("Delete", () => OnDelete());
    }

    private void OnMoveToNextSentence()
    {
        Sentence nextSentence = songEditorSceneController.GetNextSentence(uiNote.Note.Sentence);
        uiNote.Note.SetSentence(nextSentence);
        songEditorSceneController.OnNotesChanged();
    }

    private void OnMoveToPreviousSentence()
    {
        Sentence previousSentence = songEditorSceneController.GetPreviousSentence(uiNote.Note.Sentence);
        uiNote.Note.SetSentence(previousSentence);
        songEditorSceneController.OnNotesChanged();
    }

    private bool CanMoveToNextSentence()
    {
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        if (selectedNotes.Count != 1)
        {
            return false;
        }

        Note selectedNote = selectedNotes[0];
        if (selectedNote != uiNote.Note || selectedNote.Sentence == null)
        {
            return false;
        }

        // Check that the selected note is the last note in the sentence.
        List<Note> notesInSentence = new List<Note>(selectedNote.Sentence.Notes);
        notesInSentence.Sort(Note.comparerByStartBeat);
        if (notesInSentence.Last() != selectedNote)
        {
            return false;
        }

        // Check that there exists a following sentence
        Sentence nextSentence = songEditorSceneController.GetNextSentence(selectedNote.Sentence);
        return (nextSentence != null);
    }

    private bool CanMoveToPreviousSentence()
    {
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        if (selectedNotes.Count != 1)
        {
            return false;
        }

        Note selectedNote = selectedNotes[0];
        if (selectedNote != uiNote.Note || selectedNote.Sentence == null)
        {
            return false;
        }

        // Check that the selected note is the first note in the sentence.
        List<Note> notesInSentence = new List<Note>(selectedNote.Sentence.Notes);
        notesInSentence.Sort(Note.comparerByStartBeat);
        if (notesInSentence.First() != selectedNote)
        {
            return false;
        }

        // Check that there exists a following sentence
        Sentence previousSentence = songEditorSceneController.GetPreviousSentence(selectedNote.Sentence);
        return (previousSentence != null);
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

    private void OnDelete()
    {
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        foreach (Note note in selectedNotes)
        {
            songEditorSceneController.DeleteNote(note);
            songEditorSceneController.OnNotesChanged();
        }
    }
}
