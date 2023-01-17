using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteLyricsInputControl : EditorLyricsInputPopupControl
{
    [Inject]
    private EditorNoteControl editorNoteControl;

    [Inject]
    private SongEditorLayerManager layerManager;

    protected override string GetInitialText()
    {
        return ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(editorNoteControl.Note.Text);
    }

    protected override void PreviewNewText(string newText)
    {
        // Immediately apply changed lyrics to notes, but do not record it in the history.
        string visibleWhiteSpaceText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(newText);
        editorNoteControl.Note.SetText(visibleWhiteSpaceText);
        editorNoteControl.SetLyrics(visibleWhiteSpaceText);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = false});
    }

    protected override void ApplyNewText(string newText)
    {
        ApplyEditModeText(newText, true);
    }

    private void ApplyEditModeText(string newText, bool undoable)
    {
        string viewModeText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(newText);

        if (LyricsUtils.IsOnlyWhitespace(newText))
        {
            return;
        }

        // Replace multiple control characters with a single character
        viewModeText = Regex.Replace(viewModeText, @"\s+", " ");
        viewModeText = Regex.Replace(viewModeText, @";+", ";");

        // Split note to apply space and semicolon control characters.
        // Otherwise the text would mess up following notes when using the LyricsArea.
        List<Note> notesAfterSplit = SplitNoteForNewText(editorNoteControl.Note, viewModeText);

        if (notesAfterSplit.Count > 1)
        {
            // Note has been split
            songMetaChangeEventStream.OnNext(new NotesSplitEvent { Undoable = undoable});
        }
        else
        {
            viewModeText = viewModeText.Replace(";", "");
            editorNoteControl.Note.SetText(viewModeText);
            editorNoteControl.SyncWithNote();
            songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable});
        }
    }

    public List<Note> SplitNoteForNewText(Note note, string newText)
    {
        List<Note> notesAfterSplit = new List<Note> { note };
        if (note.Length <= 1
            || LyricsUtils.IsOnlyWhitespace(newText))
        {
            return notesAfterSplit;
        }

        List<int> splitIndexes = AllIndexesOfCharacterBeforeTextEnd(newText, ' ')
            .ToList()
            .Union(AllIndexesOfCharacterBeforeTextEnd(newText, ';').ToList())
            .Distinct()
            .ToList();
        splitIndexes.Sort();
        if (splitIndexes.IsNullOrEmpty())
        {
            // Nothing to split
            return notesAfterSplit;
        }

        splitIndexes = splitIndexes
            .Select(index => index + 1)
            .ToList();

        if (!splitIndexes.Contains(newText.Length))
        {
            splitIndexes.Add(newText.Length);
        }

        List<int> splitBeats = splitIndexes
            .Select(index => (int)Math.Floor(note.StartBeat + note.Length * ((double)index / newText.Length)))
            .ToList();

        // Change original note
        note.SetEndBeat(splitBeats[0]);
        note.SetText(newText.Substring(0, splitIndexes[0]));

        int lastSplitIndex = splitIndexes[0];
        int lastSplitBeat = note.EndBeat;

        // Start from 1 because original note was changed already above
        for (int i = 1; i < splitIndexes.Count; i++)
        {
            int splitIndex = splitIndexes[i];
            int splitBeat = splitBeats[i];

            int newNoteStartBeat = lastSplitBeat;
            int newNoteEndBeat = splitBeat;
            int length = splitIndex - lastSplitIndex;
            string newNoteText = newText.Substring(lastSplitIndex, length);

            Note newNote = new(note.Type, newNoteStartBeat, newNoteEndBeat - newNoteStartBeat, note.TxtPitch, newNoteText);
            notesAfterSplit.Add(newNote);
            newNote.SetSentence(note.Sentence);
            if (layerManager.TryGetEnumLayer(note, out SongEditorEnumLayer songEditorLayer))
            {
                layerManager.AddNoteToEnumLayer(songEditorLayer.LayerEnum, newNote);
            }

            lastSplitIndex = splitIndex;
            lastSplitBeat = splitBeat;
        }

        // Remove semicolon from lyrics. These are only used to separate notes in the song editor.
        notesAfterSplit.ForEach(currentNote =>
            currentNote.SetText(currentNote.Text.Replace(";", "")));

        return notesAfterSplit;
    }

    private static List<int> AllIndexesOfCharacterBeforeTextEnd(string text, char searchChar)
    {
        List<int> result = new();
        for (int i = 0; i < text.Length - 1; i++)
        {
            char c = text[i];
            if (c == searchChar)
            {
                result.Add(i);
            }
        }

        return result;
    }
}
