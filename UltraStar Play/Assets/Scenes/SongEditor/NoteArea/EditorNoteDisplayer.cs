using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UI;

#pragma warning disable CS0649

public class EditorNoteDisplayer : MonoBehaviour, INeedInjection
{

    [InjectedInInspector]
    public UiEditorNote notePrefab;
    [InjectedInInspector]
    public RectTransform noteContainer;

    [InjectedInInspector]
    public SentenceMarkerLine sentenceMarkerLinePrefab;
    [InjectedInInspector]
    public RectTransform sentenceMarkerLineContainer;

    [InjectedInInspector]
    public SentenceMarkerRectangle sentenceMarkerRectanglePrefab;
    [InjectedInInspector]
    public RectTransform sentenceMarkerRectangleContainer;

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    void Start()
    {
        noteArea.ViewportEventStream.Subscribe(_ =>
        {
            UpdateNotes();
            UpdateSentences();
        });
    }

    private void UpdateSentences()
    {
        sentenceMarkerLineContainer.DestroyAllDirectChildren();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        int minBeat = (int)Math.Floor(noteArea.GetMinBeatInViewport());
        int maxBeat = (int)Math.Ceiling(noteArea.GetMaxBeatInViewport());

        List<Sentence> sentences = voices.SelectMany(voice => voice.Sentences).ToList();

        int sentenceIndex = 0;
        foreach (Sentence sentence in sentences)
        {
            bool isInViewport = (sentence.StartBeat <= maxBeat && sentence.EndBeat >= minBeat);
            if (isInViewport)
            {
                CreateSentenceMarker(sentence, sentenceIndex + 1);
            }
            sentenceIndex++;
        }
    }

    private void UpdateNotes()
    {
        noteContainer.DestroyAllDirectChildren();

        int minBeat = (int)Math.Floor(noteArea.GetMinBeatInViewport());
        int maxBeat = (int)Math.Ceiling(noteArea.GetMaxBeatInViewport());

        int minMidiNote = noteArea.GetMinMidiNoteInViewport();
        int maxMidiNote = noteArea.GetMaxMidiNoteInViewport();

        List<Sentence> sentencesInViewport = voices
            .SelectMany(voice => voice.Sentences)
            .Where(sentence => sentence.StartBeat <= maxBeat && sentence.EndBeat >= minBeat)
            .ToList();

        List<Note> notesInViewport = sentencesInViewport
            .SelectMany(sentence => sentence.Notes)
            .Where(note => note.StartBeat <= maxBeat && note.EndBeat >= minBeat)
            .Where(note => note.MidiNote <= maxMidiNote && note.MidiNote >= minMidiNote)
            .ToList();

        foreach (Note note in notesInViewport)
        {
            CreateNote(note);
        }
    }

    private void CreateSentenceMarker(Sentence sentence, int sentenceIndex)
    {
        CreateSentenceMarkerLine(sentence.StartBeat);
        CreateSentenceMarkerLine(sentence.EndBeat);
        CreateSentenceMarkerRectangle(sentence.StartBeat, sentence.EndBeat, sentenceIndex);
    }

    private void CreateSentenceMarkerRectangle(int startBeat, int endBeat, int sentenceIndex)
    {
        SentenceMarkerRectangle sentenceMarkerRectangle = Instantiate(sentenceMarkerRectanglePrefab, sentenceMarkerRectangleContainer);
        RectTransform rectTransform = sentenceMarkerRectangle.GetComponent<RectTransform>();

        float xStart = noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = noteArea.GetHorizontalPositionForBeat(endBeat);

        rectTransform.anchorMin = new Vector2(xStart, 0);
        rectTransform.anchorMax = new Vector2(xEnd, 1);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        sentenceMarkerRectangle.GetComponentInChildren<Text>().text = sentenceIndex.ToString();
    }

    private void CreateSentenceMarkerLine(int beat)
    {
        SentenceMarkerLine sentenceMarkerLine = Instantiate(sentenceMarkerLinePrefab, sentenceMarkerLineContainer);
        RectTransform rectTransform = sentenceMarkerLine.GetComponent<RectTransform>();

        float x = noteArea.GetHorizontalPositionForBeat(beat);

        rectTransform.anchorMin = new Vector2(x, 0);
        rectTransform.anchorMax = new Vector2(x, 1);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 0);
    }

    private void CreateNote(Note note)
    {
        if (note.StartBeat == note.EndBeat)
        {
            return;
        }

        UiEditorNote uiNote = Instantiate(notePrefab, noteContainer);
        injector.Inject(uiNote);
        uiNote.Init(note);

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, note.MidiNote, note.StartBeat, note.EndBeat);
    }

    private void PositionUiNote(RectTransform uiNoteRectTransform, int midiNote, int startBeat, int endBeat)
    {
        float y = noteArea.GetVerticalPositionForMidiNote(midiNote);
        float xStart = noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = noteArea.GetHorizontalPositionForBeat(endBeat);
        float height = noteArea.GetHeightForSingleNote();

        uiNoteRectTransform.anchorMin = new Vector2(xStart, y - height / 2f);
        uiNoteRectTransform.anchorMax = new Vector2(xEnd, y + height / 2f);
        uiNoteRectTransform.anchoredPosition = Vector2.zero;
        uiNoteRectTransform.sizeDelta = Vector2.zero;
    }
}
