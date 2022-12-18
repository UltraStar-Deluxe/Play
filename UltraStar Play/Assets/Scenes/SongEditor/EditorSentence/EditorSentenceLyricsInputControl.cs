using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorSentenceLyricsInputControl : EditorLyricsInputPopupControl
{
    [Inject]
    private EditorSentenceControl editorSentenceControl;

    private string lastEditModeText;

    protected override string GetInitialText()
    {
        string text = LyricsUtils.GetEditModeText(editorSentenceControl.Sentence);
        return ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(text);
    }

    protected override void PreviewNewText(string newText)
    {
        // Immediately apply changed lyrics to notes, but do not record it in the history.
        ApplyEditModeText(newText, false);
    }

    protected override void ApplyNewText(string newText)
    {
        if (!LyricsUtils.IsOnlyWhitespace(newText))
        {
            ApplyEditModeText(newText, true);
        }
    }

    private void ApplyEditModeText(string editModeText, bool undoable)
    {
        // Map edit-mode text to lyrics of notes
        string visibleWhiteSpaceText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(editModeText);
        LyricsUtils.MapEditModeTextToNotes(visibleWhiteSpaceText, new List<Sentence> { editorSentenceControl.Sentence });
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable });
    }
}
