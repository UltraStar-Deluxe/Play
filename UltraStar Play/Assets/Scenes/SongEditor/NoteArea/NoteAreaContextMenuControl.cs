using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaContextMenuControl : ContextMenuControl
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private AddNoteAction addNoteAction;

    [Inject]
    private SetMusicGapAction setMusicGapAction;

    [Inject]
    private SetSongPropertyAction setSongPropertyAction;

    [Inject]
    private SongEditorCopyPasteManager songEditorCopyPasteManager;

    [Inject]
    private NoteAreaDragControl noteAreaDragControl;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        FillContextMenuAction = FillContextMenu;
        ShouldOpenContextMenuFunction = () =>
            editorNoteDisplayer.EditorNoteControls.AllMatch(noteControl => !noteControl.IsPointerOver)
            && editorNoteDisplayer.EditorSentenceControls.AllMatch(sentenceControl => !sentenceControl.IsPointerOver);
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenu)
    {
        int beat = (int)noteAreaControl.GetHorizontalMousePositionInBeats();
        int midiNote = noteAreaControl.GetVerticalMousePositionInMidiNote();

        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_fitViewVertically), () => noteAreaControl.FitViewportVerticalToNotes());

        Sentence sentenceAtBeat = SongMetaUtils.GetSentencesAtBeat(songMeta, beat).FirstOrDefault();
        if (sentenceAtBeat != null)
        {
            int minBeat = sentenceAtBeat.MinBeat - 1;
            int maxBeat = sentenceAtBeat.ExtendedMaxBeat + 1;
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_fitViewHorizontallyToSentence), () => noteAreaControl.FitViewportHorizontal(minBeat, maxBeat));
        }

        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.Count > 0)
        {
            int minBeat = selectedNotes.Select(it => it.StartBeat).Min() - 1;
            int maxBeat = selectedNotes.Select(it => it.EndBeat).Max() + 1;
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_fitViewHorizontallyToSelection), () => noteAreaControl.FitViewportHorizontal(minBeat, maxBeat));
        }

        if (selectedNotes.Count > 0
            || songEditorCopyPasteManager.HasCopy)
        {
            contextMenu.AddSeparator();
            if (selectedNotes.Count > 0)
            {
                contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_copyNotes), () => songEditorCopyPasteManager.CopySelection());
            }

            if (songEditorCopyPasteManager.HasCopy)
            {
                contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_pasteNotes), () => songEditorCopyPasteManager.Paste());
            }
        }

        if (selectedNotes.Count == 0)
        {
            double positionInMillis = noteAreaControl.ScreenPixelPositionToMillis(contextMenu.Position.x);

            contextMenu.AddSeparator();
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setGap), () => setMusicGapAction.ExecuteAndNotify(positionInMillis));
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setGapKeepNotePosition), () => setMusicGapAction.ExecuteAndNotify(positionInMillis, true));
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setMedleyStart), () => setSongPropertyAction.SetMedleyStartAndNotify(positionInMillis));
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setMedleyEnd), () => setSongPropertyAction.SetMedleyEndAndNotify(positionInMillis));
        }
    }
}
