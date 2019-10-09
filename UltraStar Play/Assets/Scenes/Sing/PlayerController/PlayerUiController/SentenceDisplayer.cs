using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SentenceDisplayer : MonoBehaviour
{
    // The number of lines on which notes can be placed.
    // One can imagine that notes can be placed not only on the drawn lines,
    // but also the rows between two lines.
    // 
    // This must be a multiply of 12, such that a note that is shifted by an octave
    // will be wrapped around and placed on the same line as without the shift
    // (so only relative note value is relevant).
    private int noteLineCount = 12;

    public UiNote uiNotePrefab;
    public UiRecordedNote uiRecordedNotePrefab;

    private Sentence displayedSentence;

    public void Init(int noteLineCount)
    {
        this.noteLineCount = noteLineCount;
    }

    public void DisplaySentence(Sentence sentence)
    {
        displayedSentence = sentence;

        foreach (UiNote uiNote in GetComponentsInChildren<UiNote>())
        {
            Destroy(uiNote.gameObject);
        }

        if (sentence == null)
        {
            return;
        }

        foreach (Note note in sentence.Notes)
        {
            DisplayNote(sentence, note);
        }
    }

    private void DisplayNote(Sentence sentence, Note note)
    {
        UiNote uiNote = Instantiate(uiNotePrefab);
        uiNote.transform.SetParent(transform);
        uiNote.Note = note;
        uiNote.isGolden = note.IsGolden;

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = note.Text + " (" + MidiUtils.GetAbsoluteName(note.MidiNote) + ")";

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, note.MidiNote, note.StartBeat, note.EndBeat);
    }

    public void DisplayRecordedNotes(List<RecordedNote> recordedNotes)
    {
        foreach (UiRecordedNote uiNote in GetComponentsInChildren<UiRecordedNote>())
        {
            Destroy(uiNote.gameObject);
        }

        if (recordedNotes == null)
        {
            return;
        }

        foreach (RecordedNote recordedNote in recordedNotes)
        {
            DisplayRecordedNote(recordedNote, true);
            if (recordedNote.RecordedMidiNote != recordedNote.RoundedMidiNote)
            {
                DisplayRecordedNote(recordedNote, false);
            }
        }
    }

    private void DisplayRecordedNote(RecordedNote recordedNote, bool useRoundedNote = true)
    {
        int midiNote = (useRoundedNote) ? recordedNote.RoundedMidiNote : recordedNote.RecordedMidiNote;

        UiRecordedNote uiNote = Instantiate(uiRecordedNotePrefab);
        uiNote.transform.SetParent(transform);

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = (useRoundedNote) ? MidiUtils.GetAbsoluteName(recordedNote.RoundedMidiNote)
                                           : MidiUtils.GetAbsoluteName(recordedNote.RecordedMidiNote);

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, midiNote, recordedNote.StartBeat, recordedNote.EndBeat);
    }

    private void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        // Calculate offset, such that the average note will be on the middle line
        // (thus, middle line has offset of zero).
        int offset = (noteLineCount / 2) - (((int)displayedSentence.AvgMidiNote) % noteLineCount);
        int noteLine = (offset + midiNote) % noteLineCount;

        uint sentenceStartBeat = displayedSentence.StartBeat;
        uint sentenceEndBeat = displayedSentence.EndBeat;
        uint beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        double anchorY = (double)noteLine / (double)noteLineCount;
        double anchorX = (double)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        Vector2 anchor = new Vector2((float)anchorX, (float)anchorY);
        uiNote.anchorMin = anchor;
        uiNote.anchorMax = anchor;
        uiNote.anchoredPosition = Vector2.zero;

        float length = (float)(noteEndBeat - noteStartBeat);
        uiNote.sizeDelta = new Vector2(800f * length / (float)beatsInSentence, uiNote.sizeDelta.y);
    }
}
