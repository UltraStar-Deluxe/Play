using System.Collections.Generic;
using System.Text;
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
        string text = LyricsUtils.GetEditModeText(editorNoteControl.Note);
        return ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(text);
    }

    protected override void PreviewNewText(string newText)
    {
        // Immediately apply changed lyrics to notes, but do not record it in the history.
        string whiteSpaceText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(newText);
        List<string> syllables = LyricsUtils.ParseEditable(whiteSpaceText);
        string joinedSyllables = syllables.JoinWith("");

        editorNoteControl.Note.SetText(joinedSyllables);
        editorNoteControl.SetLyrics(joinedSyllables);
        songMetaChangedEventStream.OnNext(new LyricsChangedEvent { Undoable = false });
    }

    protected override void ApplyNewText(string newText)
    {
        ApplyEditModeTextAndNotify(newText, true);
    }

    private void ApplyEditModeTextAndNotify(string newText, bool undoable)
    {
        string whiteSpaceText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(newText);

        bool wasOnLayer = layerManager.TryGetEnumLayer(editorNoteControl.Note, out SongEditorEnumLayer songEditorLayer);
        List<Note> notesAfterSplit = LyricsUtils.SplitNoteAndApplyEditModeText(editorNoteControl.Note, whiteSpaceText);
        if (wasOnLayer && !notesAfterSplit.IsNullOrEmpty())
        {
            layerManager.RemoveNoteFromAllEnumLayers(editorNoteControl.Note);
            notesAfterSplit.ForEach(newNote => layerManager.AddNoteToEnumLayer(songEditorLayer.LayerEnum, newNote));
        }

        if (notesAfterSplit.Count > 1)
        {
            // Note has been split
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(notesAfterSplit, settings.SongEditorSettings.SpaceBetweenNotesInMillis, songMeta);

            songMetaChangedEventStream.OnNext(new NotesSplitEvent() { Undoable = undoable });
        }
        else
        {
            editorNoteControl.SyncWithNote();
            songMetaChangedEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable });
        }
    }
}
