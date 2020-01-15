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
    private AddNoteAction addNoteAction;

    [Inject]
    private SongEditorSelectionController selectionController;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        int beat = (int)noteArea.GetHorizontalMousePositionInBeats();
        int midiNote = noteArea.GetVerticalMousePositionInMidiNote();

        contextMenu.AddItem("Fit vertical", () => noteArea.FitViewportVerticalToNotes());

        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        if (selectedNotes.Count > 0)
        {
            contextMenu.AddItem("Fit horizontal to selection", () => noteArea.FitViewportHorizontalToNotes(selectedNotes));
        }

        contextMenu.AddSeparator();
        contextMenu.AddItem("Add note", () => addNoteAction.ExecuteAndNotify(songMeta, beat, midiNote));
    }
}
