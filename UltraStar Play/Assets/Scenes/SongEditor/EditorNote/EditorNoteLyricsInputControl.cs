using System.Collections.Generic;
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
        return ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(editorNoteControl.Note.Text);
    }

    protected override void PreviewNewText(string newText)
    {
        // Immediately apply changed lyrics to notes, but do not record it in the history.
        string visibleWhiteSpaceText = ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(newText);
        editorNoteControl.Note.SetText(visibleWhiteSpaceText);
        editorNoteControl.SetLyrics(visibleWhiteSpaceText);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = false});
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

            songMetaChangeEventStream.OnNext(new NotesSplitEvent() { Undoable = undoable});
        }
        else
        {
            viewModeText = viewModeText.Replace(";", "");
            editorNoteControl.Note.SetText(viewModeText);
            editorNoteControl.SyncWithNote();
            songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable});
        }
    }
}
