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

    public StarParticle perfectSentenceStarPrefab;

    public RectTransform uiNotesContainer;
    public RectTransform uiRecordedNotesContainer;
    public RectTransform uiEffectsContainer;

    private readonly Dictionary<RecordedNote, UiRecordedNote> recordedNoteToUiRecordedNoteMap = new Dictionary<RecordedNote, UiRecordedNote>();

    private Sentence displayedSentence;

    private MicProfile micProfile;

    public bool displayRoundedAndActualRecordedNotes;

    public void Init(int noteLineCount, MicProfile micProfile)
    {
        this.micProfile = micProfile;
        this.noteLineCount = noteLineCount;
    }

    public void RemoveAllDisplayedNotes()
    {
        RemoveUiNotes();
        RemoveUiRecordedNotes();
    }

    public void DisplaySentence(Sentence sentence)
    {
        displayedSentence = sentence;

        RemoveAllDisplayedNotes();

        if (sentence == null)
        {
            return;
        }

        foreach (Note note in sentence.Notes)
        {
            CreateUiNote(note);
        }
    }

    public void DisplayRecordedNote(RecordedNote recordedNote)
    {
        // Remove any existing UiRecordedNote that has been drawn before for this RecordedNote.
        if (recordedNoteToUiRecordedNoteMap.TryGetValue(recordedNote, out UiRecordedNote uiRecordedNote))
        {
            recordedNoteToUiRecordedNoteMap.Remove(recordedNote);
            Destroy(uiRecordedNote.gameObject);
        }

        // Draw the bar for the rounded note
        // and draw the bar for the actually recorded pitch if needed.
        CreateUiRecordedNote(recordedNote, true);
        if (displayRoundedAndActualRecordedNotes && (recordedNote.RecordedMidiNote != recordedNote.RoundedMidiNote))
        {
            CreateUiRecordedNote(recordedNote, false);
        }
    }

    private void RemoveUiNotes()
    {
        foreach (UiNote uiNote in uiNotesContainer.GetComponentsInChildren<UiNote>())
        {
            Destroy(uiNote.gameObject);
        }
    }

    private void CreateUiNote(Note note)
    {
        UiNote uiNote = Instantiate(uiNotePrefab);
        uiNote.transform.SetParent(uiNotesContainer);
        uiNote.Init(note, uiEffectsContainer);
        if (micProfile != null)
        {
            uiNote.SetColorOfMicProfile(micProfile);
        }

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = note.Text + " (" + MidiUtils.GetAbsoluteName(note.MidiNote) + ")";

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, note.MidiNote, note.StartBeat, note.EndBeat);
    }

    private void RemoveUiRecordedNotes()
    {
        foreach (UiRecordedNote uiNote in uiRecordedNotesContainer.GetComponentsInChildren<UiRecordedNote>())
        {
            Destroy(uiNote.gameObject);
        }
        recordedNoteToUiRecordedNoteMap.Clear();
    }

    private void CreateUiRecordedNote(RecordedNote recordedNote, bool useRoundedMidiNote = true)
    {
        int midiNote = (useRoundedMidiNote) ? recordedNote.RoundedMidiNote : recordedNote.RecordedMidiNote;

        UiRecordedNote uiNote = Instantiate(uiRecordedNotePrefab);
        uiNote.transform.SetParent(uiRecordedNotesContainer);
        if (micProfile != null)
        {
            uiNote.SetColorOfMicProfile(micProfile);
        }

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = (useRoundedMidiNote) ? MidiUtils.GetAbsoluteName(recordedNote.RoundedMidiNote)
                                           : MidiUtils.GetAbsoluteName(recordedNote.RecordedMidiNote);

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, midiNote, recordedNote.StartBeat, recordedNote.EndBeat);

        recordedNoteToUiRecordedNoteMap[recordedNote] = uiNote;
    }

    private void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        // Calculate offset, such that the average note will be on the middle line
        // (thus, middle line has offset of zero).
        int offset = (noteLineCount / 2) - (((int)displayedSentence.AvgMidiNote) % noteLineCount);
        int noteLine = (offset + midiNote) % noteLineCount;

        int sentenceStartBeat = displayedSentence.StartBeat;
        int sentenceEndBeat = displayedSentence.EndBeat;
        int beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        double anchorY = (double)noteLine / (double)noteLineCount;
        double anchorX = (double)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        Vector2 anchor = new Vector2((float)anchorX, (float)anchorY);
        uiNote.anchorMin = anchor;
        uiNote.anchorMax = anchor;
        uiNote.anchoredPosition = Vector2.zero;

        float length = (float)(noteEndBeat - noteStartBeat);
        uiNote.sizeDelta = new Vector2(800f * length / (float)beatsInSentence, uiNote.sizeDelta.y);
    }

    public void CreatePerfectSentenceEffect()
    {
        for (int i = 0; i < 50; i++)
        {
            CreatePerfectSentenceStar();
        }
    }

    private void CreatePerfectSentenceStar()
    {
        StarParticle star = Instantiate(perfectSentenceStarPrefab, uiEffectsContainer);
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = UnityEngine.Random.Range(0f, 1f);
        float anchorY = UnityEngine.Random.Range(0f, 1f);
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;

        star.RectTransform.localScale = Vector3.one * UnityEngine.Random.Range(0.2f, 0.6f);
        LeanTween.scale(star.RectTransform, Vector3.zero, 1f)
            .setOnComplete(() => Destroy(star.gameObject));
    }
}
