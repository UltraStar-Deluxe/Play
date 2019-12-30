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

    [Inject(key = "voices")]
    private List<Voice> voices;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        int midiNote = noteArea.GetVerticalMousePositionInMidiNote();
        int beat = (int)noteArea.GetHorizontalMousePositionInBeats();
        contextMenu.AddItem("Fit vertical", () => noteArea.FitViewportVerticalToNotes());
        contextMenu.AddItem($"Add note", () => OnAddNote(midiNote, beat));
        contextMenu.AddItem("Add sentence", () => OnAddSentence(midiNote, beat));
    }

    private void OnAddSentence(int midiNote, int beat)
    {
        List<Sentence> sentencesAtBeat = GetSentencesAtBeat(beat);
        if (!(sentencesAtBeat.Count == 0))
        {
            return;
        }

        Note newNote = new Note(ENoteType.Normal, beat - 2, 4, 0, "~");
        newNote.SetMidiNote(midiNote);
        Sentence newSentence = new Sentence(new List<Note> { newNote }, newNote.EndBeat);
        newSentence.SetVoice(voices[0]);

        songEditorSceneController.OnNotesChanged();
    }

    private void OnAddNote(int midiNote, int beat)
    {
        List<Sentence> sentencesAtBeat = GetSentencesAtBeat(beat);
        if (sentencesAtBeat.Count == 0)
        {
            OnAddSentence(midiNote, beat);
        }
        else
        {
            Note newNote = new Note(ENoteType.Normal, beat - 2, 4, 0, "~");
            newNote.SetMidiNote(midiNote);
            newNote.SetSentence(sentencesAtBeat[0]);

            songEditorSceneController.OnNotesChanged();
        }
    }

    private List<Sentence> GetSentencesAtBeat(int beat)
    {
        return voices.SelectMany(voice => voice.Sentences)
            .Where(sentence => IsBeatInSentence(sentence, beat)).ToList();
    }

    private bool IsBeatInSentence(Sentence sentence, int beat)
    {
        return sentence.MinBeat <= beat && beat <= Math.Max(sentence.MaxBeat, sentence.LinebreakBeat);
    }
}
