using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private AddNoteAction addNoteAction;

    [Inject]
    private SetMusicGapAction setMusicGapAction;

    [Inject]
    private SongEditorCopyPasteManager songEditorCopyPasteManager;
    
    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        int beat = (int)noteArea.GetHorizontalMousePositionInBeats();
        int midiNote = noteArea.GetVerticalMousePositionInMidiNote();

        contextMenu.AddItem("Fit vertical", () => noteArea.FitViewportVerticalToNotes());

        Sentence sentenceAtBeat = SongMetaUtils.GetSentencesAtBeat(songMeta, beat).FirstOrDefault();
        if (sentenceAtBeat != null)
        {
            int minBeat = sentenceAtBeat.MinBeat - 1;
            int maxBeat = sentenceAtBeat.ExtendedMaxBeat + 1;
            contextMenu.AddItem("Fit horizontal to sentence ", () => noteArea.FitViewportHorizontal(minBeat, maxBeat));
        }

        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        if (selectedNotes.Count > 0)
        {
            int minBeat = selectedNotes.Select(it => it.StartBeat).Min() - 1;
            int maxBeat = selectedNotes.Select(it => it.EndBeat).Max() + 1;
            contextMenu.AddItem("Fit horizontal to selection", () => noteArea.FitViewportHorizontal(minBeat, maxBeat));
        }

        if (selectedNotes.Count > 0
            || songEditorCopyPasteManager.CopiedNotes.Count > 0)
        {
            contextMenu.AddSeparator();
            if (selectedNotes.Count > 0)
            {
                contextMenu.AddItem("Copy notes", () => songEditorCopyPasteManager.CopySelectedNotes());
            }

            if (songEditorCopyPasteManager.CopiedNotes.Count > 0)
            {
                contextMenu.AddItem("Paste notes", () => songEditorCopyPasteManager.PasteCopiedNotes());
            }
        }
        
        contextMenu.AddSeparator();
        contextMenu.AddItem("Add note", () => addNoteAction.ExecuteAndNotify(songMeta, beat, midiNote));

        if (selectedNotes.Count == 0)
        {
            contextMenu.AddSeparator();
            contextMenu.AddItem("Set Gap to playback position", () => setMusicGapAction.ExecuteAndNotify());
        }
    }
}
