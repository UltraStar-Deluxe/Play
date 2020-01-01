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
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private NoteArea noteArea;

    private List<Voice> Voices
    {
        get
        {
            return songEditorSceneController.Voices;
        }
    }

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        int midiNote = noteArea.GetVerticalMousePositionInMidiNote();
        int beat = (int)noteArea.GetHorizontalMousePositionInBeats();
        contextMenu.AddItem("Fit vertical", () => noteArea.FitViewportVerticalToNotes());
        contextMenu.AddItem($"Add note", () => OnAddNote(midiNote, beat));
    }

    private void OnAddNote(int midiNote, int beat)
    {
        List<Sentence> sentencesAtBeat = songEditorSceneController.GetSentencesAtBeat(beat);
        if (sentencesAtBeat.Count == 0)
        {
            // Add sentence with note
            Note newNote = new Note(ENoteType.Normal, beat - 2, 4, 0, "~");
            newNote.SetMidiNote(midiNote);
            Sentence newSentence = new Sentence(new List<Note> { newNote }, newNote.EndBeat);
            newSentence.SetVoice(Voices[0]);

            songEditorSceneController.OnNotesChanged();
        }
        else
        {
            // Add note to existing sentence
            Note newNote = new Note(ENoteType.Normal, beat - 2, 4, 0, "~");
            newNote.SetMidiNote(midiNote);
            newNote.SetSentence(sentencesAtBeat[0]);

            songEditorSceneController.OnNotesChanged();
        }
    }
}
