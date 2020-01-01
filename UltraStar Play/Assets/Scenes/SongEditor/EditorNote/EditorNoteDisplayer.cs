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
    public EditorUiSentence sentenceMarkerRectanglePrefab;

    [InjectedInInspector]
    public RectTransform sentenceMarkerRectangleContainer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    private List<Voice> Voices
    {
        get
        {
            return songEditorSceneController.Voices;
        }
    }

    private List<Sentence> sortedSentencesOfAllVoices = new List<Sentence>();

    private Dictionary<Voice, List<Sentence>> voiceToSortedSentencesMap = new Dictionary<Voice, List<Sentence>>();

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private Settings settings;

    private readonly List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorUiNote> noteToEditorUiNoteMap = new Dictionary<Note, EditorUiNote>();

    void Start()
    {
        noteContainer.DestroyAllDirectChildren();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        ReloadSentences();

        noteArea.ViewportEventStream.Subscribe(_ =>
        {
            UpdateNotesAndSentences();
        });

        foreach (ESongEditorLayer layer in EnumUtils.GetValuesAsList<ESongEditorLayer>())
        {
            songEditorLayerManager
                .ObserveEveryValueChanged(it => it.IsLayerEnabled(layer))
                .Subscribe(_ => UpdateNotes());
        }

        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.HideVoices.Count).Subscribe(_ => OnHideVoicesChanged());
    }

    private void OnHideVoicesChanged()
    {
        // Remove notes of hidden voices
        foreach (EditorUiNote uiNote in new List<EditorUiNote>(noteToEditorUiNoteMap.Values))
        {
            if (!IsVoiceVisible(uiNote.Note.Sentence?.Voice))
            {
                DeleteUiNote(uiNote);
            }
        }

        // Draw any notes that are now (again) visible.
        UpdateNotesAndSentences();
    }

    public bool IsVoiceVisible(Voice voice)
    {
        if (voice == null)
        {
            return true;
        }
        bool isHidden = settings.SongEditorSettings.HideVoices.Contains(voice.Name)
            || voice.Name.IsNullOrEmpty() && settings.SongEditorSettings.HideVoices.Contains("P1");
        return !isHidden;
    }

    public void ClearUiNotes()
    {
        noteContainer.DestroyAllDirectChildren();
        noteToEditorUiNoteMap.Clear();
    }

    public void ReloadSentences()
    {
        voiceToSortedSentencesMap.Clear();
        sortedSentencesOfAllVoices.Clear();
        foreach (Voice voice in Voices)
        {
            sortedSentencesOfAllVoices.AddRange(voice.Sentences);
            voiceToSortedSentencesMap.Add(voice, new List<Sentence>(voice.Sentences));
        }

        sortedSentencesOfAllVoices.Sort(Sentence.comparerByStartBeat);
        foreach (List<Sentence> sentences in voiceToSortedSentencesMap.Values)
        {
            sentences.Sort(Sentence.comparerByStartBeat);
        }
    }

    public void UpdateNotesAndSentences()
    {
        UpdateNotes();
        UpdateSentences();
    }

    public void UpdateSentences()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        sentenceLinesImage.ClearTexture();
        sentenceMarkerRectangleContainer.DestroyAllDirectChildren();

        foreach (Voice voice in Voices)
        {
            bool isVisible = IsVoiceVisible(voice);
            if (isVisible)
            {
                DrawSentences(voice);
            }
        }

        sentenceLinesImage.ApplyTexture();
    }

    private void DrawSentences(Voice voice)
    {
        string voiceNamePrefix = (!voice.Name.IsNullOrEmpty()) ? voice.Name + " - " : "";
        int viewportWidthInBeats = noteArea.MaxBeatInViewport - noteArea.MinBeatInViewport;
        int sentenceIndex = 0;
        foreach (Sentence sentence in voiceToSortedSentencesMap[voice])
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

                string label = voiceNamePrefix + (sentenceIndex + 1).ToString();
                CreateUiSentence(sentence, startBeat, endBeat, label);
            }
            sentenceIndex++;
        }
    }

    public void UpdateNotes()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        DestroyUiNotesOutsideOfViewport();

        DrawNotesInSongFile();
        DrawNotesInLayers();
    }

    public EditorUiNote GetUiNoteForNote(Note note)
    {
        if (noteToEditorUiNoteMap.TryGetValue(note, out EditorUiNote uiNote))
        {
            return uiNote;
        }
        else
        {
            return null;
        }
    }

    public void DeleteUiNote(EditorUiNote uiNote)
    {
        noteToEditorUiNoteMap.Remove(uiNote.Note);
        Destroy(uiNote.gameObject);
    }

    public void DeleteNote(Note note)
    {
        if (noteToEditorUiNoteMap.TryGetValue(note, out EditorUiNote uiNote))
        {
            DeleteUiNote(uiNote);
        }
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
        .Where(sentence => IsVoiceVisible(sentence.Voice))
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

    private void CreateUiSentence(Sentence sentence, int startBeat, int endBeat, string label)
    {
        EditorUiSentence uiSentence = Instantiate(sentenceMarkerRectanglePrefab, sentenceMarkerRectangleContainer);
        RectTransform rectTransform = uiSentence.GetComponent<RectTransform>();

        injector.Inject(uiSentence);
        injector.Inject(uiSentence.GetComponent<EditorSentenceContextMenuHandler>());
        uiSentence.Init(sentence);

        float xStart = (float)noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = (float)noteArea.GetHorizontalPositionForBeat(endBeat);

        rectTransform.anchorMin = new Vector2(xStart, 0);
        rectTransform.anchorMax = new Vector2(xEnd, 1);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        uiSentence.GetComponentInChildren<Text>().text = label;
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
            injector.Inject(editorUiNote.GetComponent<EditorNoteContextMenuHandler>());
            editorUiNote.Init(note);

            noteToEditorUiNoteMap.Add(note, editorUiNote);
        }
        else
        {
            editorUiNote.SyncWithNote();
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
