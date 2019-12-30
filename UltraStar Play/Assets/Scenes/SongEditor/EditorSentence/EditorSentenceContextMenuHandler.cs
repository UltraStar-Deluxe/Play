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
    private SongEditorSceneController songEditorSceneController;

    private EditorUiSentence uiSentence;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        if (uiSentence == null)
        {
            uiSentence = GetComponent<EditorUiSentence>();
        }

        contextMenu.AddItem("Fit to notes", () => OnFitToNotes());
        contextMenu.AddSeparator();
        contextMenu.AddItem("Delete", () => OnDelete());
    }

    private void OnDelete()
    {
        songEditorSceneController.DeleteSentence(uiSentence.Sentence);
        songEditorSceneController.OnNotesChanged();
    }

    private void OnFitToNotes()
    {
        uiSentence.Sentence.UpdateMinAndMaxBeat();
        uiSentence.Sentence.SetLinebreakBeat(uiSentence.Sentence.MaxBeat);
        songEditorSceneController.OnNotesChanged();
    }
}
