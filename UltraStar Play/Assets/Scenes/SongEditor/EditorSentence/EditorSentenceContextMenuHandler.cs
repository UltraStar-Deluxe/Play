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

public class EditorSentenceContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private DeleteSentencesAction deleteSentencesAction;

    [Inject]
    private SentenceFitToNoteAction sentenceFitToNoteAction;

    private EditorUiSentence uiSentence;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        if (uiSentence == null)
        {
            uiSentence = GetComponent<EditorUiSentence>();
        }

        List<Sentence> selectedSentences = new List<Sentence> { uiSentence.Sentence };

        contextMenu.AddItem("Fit to notes", () => sentenceFitToNoteAction.ExecuteAndNotify(selectedSentences));
        contextMenu.AddSeparator();
        contextMenu.AddItem("Delete", () => deleteSentencesAction.ExecuteAndNotify(selectedSentences));
    }
}
