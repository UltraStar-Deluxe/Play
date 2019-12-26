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
    public EditorUiNote notePrefab;
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

    private List<Sentence> sentencesOfAllVoices;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    private List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorUiNote> noteToEditorUiNoteMap = new Dictionary<Note, EditorUiNote>();

    void Start()
    {
        noteContainer.DestroyAllDirectChildren();
        sentenceMarkerLineContainer.DestroyAllDirectChildren();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        sentencesOfAllVoices = voices.SelectMany(voice => voice.Sentences).ToList();

        noteArea.ViewportEventStream.Subscribe(_ =>
        {
            UpdateNotes();
            UpdateSentences();
        });

        foreach (ESongEditorLayer layer in EnumUtils.GetValuesAsList<ESongEditorLayer>())
        {
            songEditorLayerManager
                .ObserveEveryValueChanged(it => it.IsLayerEnabled(layer))
                .Subscribe(_ => UpdateNotes());
        }
    }

    private void UpdateSentences()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        sentenceMarkerLineContainer.DestroyAllDirectChildren();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        int sentenceIndex = 0;
        foreach (Sentence sentence in sentencesOfAllVoices)
        {
            if (noteArea.IsInViewport(sentence))
            {
                CreateSentenceMarker(sentence, sentenceIndex + 1);
            }
            sentenceIndex++;
        }
    }

    private void UpdateNotes()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        DestroyUiNotesOutsideOfViewport();

        DrawNotesInSongFile();
        DrawNotesInLayers();
    }

    private void DestroyUiNotesOutsideOfViewport()
    {
        ICollection<EditorUiNote> editorUiNotes = new List<EditorUiNote>(noteToEditorUiNoteMap.Values);
        foreach (EditorUiNote editorUiNote in editorUiNotes)
        {
            Note note = editorUiNote.Note;
            if (!noteArea.IsInViewport(note))
            {
                Destroy(editorUiNote.gameObject);
                noteToEditorUiNoteMap.Remove(note);
            }
        }
    }

    private void DrawNotesInLayers()
    {
        foreach (ESongEditorLayer layerKey in songEditorLayerKeys)
        {
            if (songEditorLayerManager.IsLayerEnabled(layerKey))
            {
                DrawNotesInLayer(layerKey);
            }
        }
    }

    private void DrawNotesInLayer(ESongEditorLayer layerKey)
    {
        List<Note> notesInLayer = songEditorLayerManager.GetNotes(layerKey);
        List<Note> notesInViewport = notesInLayer
            .Where(note => noteArea.IsInViewport(note))
            .ToList();

        Color layerColor = songEditorLayerManager.GetColor(layerKey);
        foreach (Note note in notesInViewport)
        {
            EditorUiNote uiNote = UpdateOrCreateNote(note);
            if (uiNote != null)
            {
                uiNote.SetColor(layerColor);
            }
        }
    }

    private void DrawNotesInSongFile()
    {
        List<Sentence> sentencesInViewport = sentencesOfAllVoices
        .Where(sentence => noteArea.IsInViewport(sentence))
        .ToList();

        List<Note> notesInViewport = sentencesInViewport
                .SelectMany(sentence => sentence.Notes)
                .Where(note => noteArea.IsInViewport(note))
                .ToList();

        foreach (Note note in notesInViewport)
        {
            UpdateOrCreateNote(note);
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

    private EditorUiNote UpdateOrCreateNote(Note note)
    {
        if (!noteToEditorUiNoteMap.TryGetValue(note, out EditorUiNote editorUiNote))
        {
            editorUiNote = Instantiate(notePrefab, noteContainer);
            injector.Inject(editorUiNote);
            editorUiNote.Init(note);

            noteToEditorUiNoteMap.Add(note, editorUiNote);
        }

        PositionUiNote(editorUiNote.RectTransform, note.MidiNote, note.StartBeat, note.EndBeat);

        return editorUiNote;
    }

    private void PositionUiNote(RectTransform uiNoteRectTransform, int midiNote, int startBeat, int endBeat)
    {
        float y = noteArea.GetVerticalPositionForMidiNote(midiNote);
        float xStart = noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = noteArea.GetHorizontalPositionForBeat(endBeat);
        float height = noteArea.HeightForSingleNote;

        uiNoteRectTransform.anchorMin = new Vector2(xStart, y - height / 2f);
        uiNoteRectTransform.anchorMax = new Vector2(xEnd, y + height / 2f);
        uiNoteRectTransform.anchoredPosition = Vector2.zero;
        uiNoteRectTransform.sizeDelta = Vector2.zero;
    }
}
