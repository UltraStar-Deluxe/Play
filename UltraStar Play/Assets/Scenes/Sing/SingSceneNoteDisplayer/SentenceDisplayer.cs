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
    [Inject]
    private PlayerControl playerControl;

    private Sentence currentSentence;

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

    public override void Init(int lineCount)
    {
        base.Init(lineCount);

        playerControl.EnterSentenceEventStream.Subscribe(enterSentenceEvent =>
        {
            DisplaySentence(enterSentenceEvent.Sentence);
        });
    }

    private void DisplaySentence(Sentence sentence)
    {
        currentSentence = sentence;
        RemoveAllDisplayedNotes();
        if (sentence == null)
        {
            return;
        }

        avgMidiNote = currentSentence.Notes.Count > 0
            ? (int)currentSentence.Notes.Select(it => it.MidiNote).Average()
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

    public override void DisplayRecordedNote(RecordedNote recordedNote)
    {
        if (currentSentence == null
            || recordedNote.TargetSentence != currentSentence
            || (recordedNote.TargetNote != null && recordedNote.TargetNote.Sentence != currentSentence))
        {
            // This is probably a recorded note from the previous sentence that is still continued because of the mic delay.
            // Do not draw the recorded note, it is not in the displayed sentence.
            return;
        }

        base.DisplayRecordedNote(recordedNote);
    }

    override protected void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        int sentenceStartBeat = currentSentence.MinBeat;
        int sentenceEndBeat = currentSentence.MaxBeat;
        int beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        Vector2 anchorY = GetAnchorYForMidiNote(midiNote);
        float anchorXStart = (float)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        float anchorXEnd = (float)(noteEndBeat - sentenceStartBeat) / beatsInSentence;

        uiNote.anchorMin = new Vector2(anchorXStart, anchorY.x);
        uiNote.anchorMax = new Vector2(anchorXEnd, anchorY.y);
        uiNote.MoveCornersToAnchors();
    }
}
