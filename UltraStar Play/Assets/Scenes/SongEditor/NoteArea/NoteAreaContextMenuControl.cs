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
    private SongMetaChangeEventStream songMetaChangeEventStream;

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

        contextMenu.AddButton("Fit vertical", () => noteAreaControl.FitViewportVerticalToNotes());

        Sentence sentenceAtBeat = SongMetaUtils.GetSentencesAtBeat(songMeta, beat).FirstOrDefault();
        if (sentenceAtBeat != null)
        {
            int minBeat = sentenceAtBeat.MinBeat - 1;
            int maxBeat = sentenceAtBeat.ExtendedMaxBeat + 1;
            contextMenu.AddButton("Fit horizontal to sentence ", () => noteAreaControl.FitViewportHorizontal(minBeat, maxBeat));
        }

        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.Count > 0)
        {
            int minBeat = selectedNotes.Select(it => it.StartBeat).Min() - 1;
            int maxBeat = selectedNotes.Select(it => it.EndBeat).Max() + 1;
            contextMenu.AddButton("Fit horizontal to selection", () => noteAreaControl.FitViewportHorizontal(minBeat, maxBeat));
        }

        if (selectedNotes.Count > 0
            || songEditorCopyPasteManager.HasCopiedNotes)
        {
            contextMenu.AddSeparator();
            if (selectedNotes.Count > 0)
            {
                contextMenu.AddButton("Copy notes", () => songEditorCopyPasteManager.CopySelectedNotes());
            }

            if (songEditorCopyPasteManager.HasCopiedNotes)
            {
                contextMenu.AddButton("Paste notes", () => songEditorCopyPasteManager.PasteCopiedNotes());
            }
        }
        
        if (selectedNotes.Count == 0)
        {
            double positionInSongInMillis = noteAreaControl.ScreenPixelPositionToMillis(contextMenu.Position.x);
            
            contextMenu.AddSeparator();
            contextMenu.AddButton("Set GAP", () => setMusicGapAction.ExecuteAndNotify(positionInSongInMillis));
            contextMenu.AddButton("Set Medley Start", () => setSongPropertyAction.SetMedleyStartAndNotify(positionInSongInMillis));
            contextMenu.AddButton("Set Medley End", () => setSongPropertyAction.SetMedleyEndAndNotify(positionInSongInMillis));
        }
    }
}
