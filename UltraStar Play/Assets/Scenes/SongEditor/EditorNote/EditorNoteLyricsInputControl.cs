using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteLyricsInputControl : EditorLyricsInputPopupControl
{
    [Inject]
    private Settings settings;
    
    [Inject]
    private SongMeta songMeta;
    
    [Inject]
    private EditorNoteControl editorNoteControl;

    [Inject]
    private SongEditorLayerManager layerManager;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        textField.SelectAll();
    }
    
    protected override string GetInitialText()
    {
        return ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(
            Regex.Replace(
                editorNoteControl.Note.Text
                    .Replace("\\", "\\\\")
                    .Replace(";", "\\;"),
                // Regex to match spaces that are not at the ends of the string
                @"(?<!^) (?!$)", "\\ "
            )
        );
    }

    protected override void PreviewNewText(string newText)
    {
        // Immediately apply changed lyrics to notes, but do not record it in the history.
        string whiteSpaceText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(newText);
        StringBuilder stringBuilder = new();
        bool escapeInProgress = false;
        foreach (char c in whiteSpaceText)
        {
            if (c == '\\' && !escapeInProgress)
            {
                escapeInProgress = true;
            }
            else if (c == ';' && !escapeInProgress)
            {
                escapeInProgress = false;
            }
            else
            {
                escapeInProgress = false;
                stringBuilder.Append(c);
            }
        }
        editorNoteControl.Note.SetText(stringBuilder.ToString());
        editorNoteControl.SetLyrics(stringBuilder.ToString());
        songMetaChangedEventStream.OnNext(new LyricsChangedEvent { Undoable = false});
    }

    protected override void ApplyNewText(string newText)
    {
        ApplyEditModeTextAndNotify(newText, true);
    }

    private void ApplyEditModeTextAndNotify(string newText, bool undoable)
    {
        string viewModeText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(newText);
        
        bool wasOnLayer = layerManager.TryGetEnumLayer(editorNoteControl.Note, out SongEditorEnumLayer songEditorLayer);
        EditLyricsUtils.TryApplyEditModeText(songMeta, editorNoteControl.Note, newText, out List<Note> notesAfterSplit);
        if (wasOnLayer
            && !notesAfterSplit.IsNullOrEmpty())
        {
            layerManager.RemoveNoteFromAllEnumLayers(editorNoteControl.Note);
            notesAfterSplit.ForEach(newNote => layerManager.AddNoteToEnumLayer(songEditorLayer.LayerEnum, newNote));
        }
        
        if (notesAfterSplit.Count > 1)
        {
            // Note has been split
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(notesAfterSplit, settings.SongEditorSettings.SpaceBetweenNotesInMillis, songMeta);

            songMetaChangedEventStream.OnNext(new NotesSplitEvent() { Undoable = undoable});
        }
        else
        {
            editorNoteControl.SyncWithNote();
            songMetaChangedEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable});
        }
    }
}
