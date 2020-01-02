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
    public DynamicallyCreatedImage sentenceLinesImage;

    [InjectedInInspector]
    public SentenceMarkerRectangle sentenceMarkerRectanglePrefab;
    [InjectedInInspector]
    public RectTransform sentenceMarkerRectangleContainer;

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    private List<Sentence> sortedSentencesOfAllVoices;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    private readonly List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorUiNote> noteToEditorUiNoteMap = new Dictionary<Note, EditorUiNote>();

    void Start()
    {
        noteContainer.DestroyAllDirectChildren();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        sortedSentencesOfAllVoices = voices.SelectMany(voice => voice.Sentences).ToList();
        sortedSentencesOfAllVoices.Sort(Sentence.comparerByStartBeat);

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
        sentenceLinesImage.ClearTexture();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        int viewportWidthInBeats = noteArea.MaxBeatInViewport - noteArea.MinBeatInViewport;

        int sentenceIndex = 0;
        foreach (Sentence sentence in sortedSentencesOfAllVoices)
        {
            if (noteArea.IsInViewport(sentence))
            {
                int startBeat = sentence.MinBeat;
                int endBeat = Math.Max(sentence.MaxBeat, sentence.LinebreakBeat);

                // Do not draw the sentence marker lines, when there are too many beats
                if (viewportWidthInBeats < 1200)
                {
                    CreateSentenceMarkerLine(startBeat, Colors.saddleBrown, 0);
                    CreateSentenceMarkerLine(endBeat, Colors.black, 20);
                }

                string label = (sentenceIndex + 1).ToString();
                CreateSentenceMarkerRectangle(startBeat, endBeat, label);
            }
            sentenceIndex++;
        }

        sentenceLinesImage.ApplyTexture();
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
        List<Sentence> sentencesInViewport = sortedSentencesOfAllVoices
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

    private void CreateSentenceMarkerRectangle(int startBeat, int endBeat, string label)
    {
        SentenceMarkerRectangle sentenceMarkerRectangle = Instantiate(sentenceMarkerRectanglePrefab, sentenceMarkerRectangleContainer);
        RectTransform rectTransform = sentenceMarkerRectangle.GetComponent<RectTransform>();

        float xStart = (float)noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = (float)noteArea.GetHorizontalPositionForBeat(endBeat);

        rectTransform.anchorMin = new Vector2(xStart, 0);
        rectTransform.anchorMax = new Vector2(xEnd, 1);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        sentenceMarkerRectangle.GetComponentInChildren<Text>().text = label;
    }

    private void CreateSentenceMarkerLine(int beat, Color color, int yDashOffset)
    {
        double beatPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        if (beatPosInMillis < noteArea.ViewportX || beatPosInMillis > noteArea.MaxMillisecondsInViewport)
        {
            return;
        }

        double xPercent = (beatPosInMillis - noteArea.ViewportX) / noteArea.ViewportWidth;
        int x = (int)(xPercent * sentenceLinesImage.TextureWidth);

        for (int y = 0; y < sentenceLinesImage.TextureHeight; y++)
        {
            // Make it dashed
            if (((y + yDashOffset) % 40) < 20)
            {
                sentenceLinesImage.SetPixel(x, y, color);
                // Make it 2 pixels wide
                if (x < sentenceLinesImage.TextureWidth - 1)
                {
                    sentenceLinesImage.SetPixel(x + 1, y, color);
                }
            }
        }
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
        float y = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        float xStart = (float)noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = (float)noteArea.GetHorizontalPositionForBeat(endBeat);
        float height = noteArea.HeightForSingleNote;

        uiNoteRectTransform.anchorMin = new Vector2(xStart, y - height / 2f);
        uiNoteRectTransform.anchorMax = new Vector2(xEnd, y + height / 2f);
        uiNoteRectTransform.anchoredPosition = Vector2.zero;
        uiNoteRectTransform.sizeDelta = Vector2.zero;
    }
}
