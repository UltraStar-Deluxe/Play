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
}
