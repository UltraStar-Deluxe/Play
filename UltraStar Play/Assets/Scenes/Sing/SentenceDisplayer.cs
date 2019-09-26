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
    public const int NoteLineCount = 24;

    public UiNote uiNotePrefab;
    public UiRecordedNote uiRecordedNotePrefab;

    private SongMeta songMeta;

    private int sentenceIndex;
    private Voice voice;
    public Sentence CurrentSentence { get; set; }
    public Sentence NextSentence { get; set; }

    public LyricsDisplayer LyricsDisplayer { get; set; }

    public void SetCurrentBeat(double currentBeat)
    {
        if (songMeta == null || voice == null || CurrentSentence == null)
        {
            return;
        }

        // Change the sentence, when the current beat is over its last note.
        if (voice.Sentences.Count > sentenceIndex - 1)
        {
            if ((uint)currentBeat > CurrentSentence.EndBeat)
            {
                sentenceIndex++;
                UpdateSentences(sentenceIndex);
            }
        }
    }

    public void LoadVoice(SongMeta songMeta, string voiceIdentifier)
    {
        this.songMeta = songMeta;

        string filePath = this.songMeta.Directory + Path.DirectorySeparatorChar + this.songMeta.Filename;
        Debug.Log($"Loading voice of {filePath}");
        Dictionary<string, Voice> voices = VoicesBuilder.ParseFile(filePath, this.songMeta.Encoding, new List<string>());
        if (string.IsNullOrEmpty(voiceIdentifier))
        {
            voice = voices.Values.First();
        }
        else
        {
            if (!voices.TryGetValue(voiceIdentifier, out voice))
            {
                throw new Exception($"The song does not contain a voice for {voiceIdentifier}");
            }
        }

        sentenceIndex = 0;
        UpdateSentences(sentenceIndex);
    }

    private void UpdateSentences(int currentSentenceIndex)
    {
        if (currentSentenceIndex < voice.Sentences.Count - 1)
        {
            CurrentSentence = voice.Sentences[currentSentenceIndex];
        }
        else
        {
            CurrentSentence = null;
        }

        int nextSentenceIndex = currentSentenceIndex + 1;
        if (nextSentenceIndex < voice.Sentences.Count - 1)
        {
            NextSentence = voice.Sentences[nextSentenceIndex];
        }
        else
        {
            NextSentence = null;
        }

        DisplayNotes(CurrentSentence);
        DisplayRecordedNotes(null);
        if (LyricsDisplayer != null)
        {
            LyricsDisplayer.SetCurrentSentence(CurrentSentence);
            LyricsDisplayer.SetNextSentence(NextSentence);
        }
    }

    private void DisplayNotes(Sentence sentence)
    {
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

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = note.Text;

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, note.MidiNote, note.StartBeat, note.EndBeat);
    }

    public void DisplayRecordedNotes(RecordedSentence recordedSentence)
    {
        foreach (UiRecordedNote uiNote in GetComponentsInChildren<UiRecordedNote>())
        {
            Destroy(uiNote.gameObject);
        }

        if (recordedSentence == null)
        {
            return;
        }

        // Debug.Log("drawing recorded notes");
        foreach (RecordedNote recordedNote in recordedSentence.RecordedNotes)
        {
            DisplayRecordedNote(recordedSentence, recordedNote);
        }
    }

    private void DisplayRecordedNote(RecordedSentence recordedSentence, RecordedNote recordedNote)
    {
        UiRecordedNote uiNote = Instantiate(uiRecordedNotePrefab);
        uiNote.transform.SetParent(transform);

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = MidiUtils.MidiNoteToAbsoluteName(recordedNote.MidiNote);

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, recordedNote.MidiNote, recordedNote.StartBeat, recordedNote.EndBeat);
    }

    private void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        // Calculate offset, such that the average note will be on the middle line
        // (thus, middle line has offset of zero).
        int offset = (NoteLineCount / 2) - (((int)CurrentSentence.AvgMidiNote) % NoteLineCount);
        int noteLine = (offset + midiNote) % NoteLineCount;

        uint sentenceStartBeat = CurrentSentence.StartBeat;
        uint sentenceEndBeat = CurrentSentence.EndBeat;
        uint beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        double anchorY = (double)noteLine / (double)NoteLineCount;
        double anchorX = (double)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        Vector2 anchor = new Vector2((float)anchorX, (float)anchorY);
        uiNote.anchorMin = anchor;
        uiNote.anchorMax = anchor;
        uiNote.anchoredPosition = Vector2.zero;

        float length = (float)(noteEndBeat - noteStartBeat);
        uiNote.sizeDelta = new Vector2(800f * length / (float)beatsInSentence, uiNote.sizeDelta.y);
    }
}
