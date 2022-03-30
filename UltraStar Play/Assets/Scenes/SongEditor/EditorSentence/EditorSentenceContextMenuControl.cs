﻿using System.Collections.Generic;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorSentenceContextMenuControl : ContextMenuControl
{
    [Inject]
    private DeleteSentencesAction deleteSentencesAction;

    [Inject]
    private SentenceFitToNoteAction sentenceFitToNoteAction;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private EditorSentenceControl sentenceControl;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        FillContextMenuAction = FillContextMenu;
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenu)
    {
        List<Sentence> selectedSentences = new() { sentenceControl.Sentence };

        contextMenu.AddItem("Fit to notes", () => sentenceFitToNoteAction.ExecuteAndNotify(selectedSentences));
        contextMenu.AddItem("Fit to notes (all phrases)", () => sentenceFitToNoteAction.ExecuteAndNotify(SongMetaUtils.GetAllSentences(songMeta)));
        contextMenu.AddSeparator();
        contextMenu.AddItem("Delete", () => deleteSentencesAction.ExecuteAndNotify(selectedSentences));
    }
}
