using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteLyricsInputControl : EditorLyricsInputPopupControl
{
    [Inject]
    private EditorNoteControl editorNoteControl;

    protected override string GetInitialText()
    {
        return ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(editorNoteControl.Note.Text);
    }

    protected override void PreviewNewText(string newText)
    {
        // Immediately apply changed lyrics to notes, but do not record it in the history.
        ApplyEditModeText(newText, false);
    }

    protected override void ApplyNewText(string newText)
    {
        ApplyEditModeText(newText, true);
    }

    private void ApplyEditModeText(string newText, bool undoable)
    {
        string viewModeText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(newText);

        // Replace multiple control characters with a single character
        viewModeText = Regex.Replace(viewModeText, @"\s+", " ");
        viewModeText = Regex.Replace(viewModeText, @";+", ";");

        // Replace any text after control characters.
        // Otherwise the text would mess up following notes when using the LyricsArea.
        viewModeText = Regex.Replace(viewModeText, @" .+", " ");
        viewModeText = Regex.Replace(viewModeText, @";.+", ";");

        // Remove the semicolon to separate notes. In contrast, a leading / trailing space needs to be preserved.
        viewModeText = viewModeText.Replace(";", "");

        if (!LyricsUtils.IsOnlyWhitespace(newText))
        {
            string visibleWhiteSpaceText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(viewModeText);
            editorNoteControl.Note.SetText(visibleWhiteSpaceText);
            editorNoteControl.SetLyrics(visibleWhiteSpaceText);
            songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable});
        }
    }

    public static void MapTextToNotes(string text, List<Note> notes, ISyllableSplitter syllableSplitter)
    {
        string[] words = text.Split(" ");

        // Map words to notes alternatingly from start and end
        int noteIndex = 0;
        foreach (string word in words)
        {
            List<string> syllables = syllableSplitter != null
                ? syllableSplitter.GetSyllables(word)
                : new List<string> { word };
            for (int syllableIndex = 0; syllableIndex < syllables.Count; syllableIndex++)
            {
                if (noteIndex >= notes.Count)
                {
                    return;
                }

                if (syllableIndex == syllables.Count - 1)
                {
                    // Add space for end of word
                    notes[noteIndex].SetText(syllables[syllableIndex] + " ");
                }
                else
                {
                    notes[noteIndex].SetText(syllables[syllableIndex]);
                }
                noteIndex++;
            }
        }

        // Remove text of notes that did not receive any new text
        for (int i = noteIndex; i < notes.Count; i++)
        {
            notes[i].SetText("_");
        }
    }
}
