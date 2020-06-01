using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SentenceDisplayer : AbstractSingSceneNoteDisplayer
{
    void Update()
    {
        // Draw the UiRecordedNotes smoothly from their StartBeat to TargetEndBeat
        foreach (UiRecordedNote uiRecordedNote in uiRecordedNotes)
        {
            if (uiRecordedNote.EndBeat < uiRecordedNote.TargetEndBeat)
            {
                UpdateUiRecordedNoteEndBeat(uiRecordedNote);
                PositionUiNote(uiRecordedNote.RectTransform, uiRecordedNote.MidiNote, uiRecordedNote.StartBeat, uiRecordedNote.EndBeat);
            }
        }
    }

    override public void DisplaySentence(Sentence sentence, Sentence nextSentence)
    {
        displayedSentence = sentence;
        RemoveAllDisplayedNotes();
        if (sentence == null)
        {
            return;
        }

        avgMidiNote = displayedSentence.Notes.Count > 0
            ? (int)displayedSentence.Notes.Select(it => it.MidiNote).Average()
            : 0;
        // The division is rounded down on purpose (e.g. noteRowCount of 3 will result in (noteRowCount / 2) == 1)
        maxNoteRowMidiNote = avgMidiNote + (noteRowCount / 2);
        minNoteRowMidiNote = avgMidiNote - (noteRowCount / 2);
        // Freestyle notes are not drawn
        IEnumerable<Note> nonFreestyleNotes = sentence.Notes.Where(note => !note.IsFreestyle);
        foreach (Note note in nonFreestyleNotes)
        {
            CreateUiNote(note);
        }
    }

    override protected void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        int noteRow = CalculateNoteRow(midiNote);

        int sentenceStartBeat = displayedSentence.MinBeat;
        int sentenceEndBeat = displayedSentence.MaxBeat;
        int beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        Vector2 anchorY = GetAnchorYForMidiNote(midiNote);
        float anchorXStart = (float)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        float anchorXEnd = (float)(noteEndBeat - sentenceStartBeat) / beatsInSentence;

        uiNote.anchorMin = new Vector2(anchorXStart, anchorY.x);
        uiNote.anchorMax = new Vector2(anchorXEnd, anchorY.y);
        uiNote.MoveCornersToAnchors();
    }
}
