using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UI;

#pragma warning disable CS0649

public class EditorNoteDisplayer : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
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
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private Settings settings;

    private readonly Dictionary<Voice, List<Sentence>> voiceToSortedSentencesMap = new Dictionary<Voice, List<Sentence>>();

    private readonly List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorUiNote> noteToEditorUiNoteMap = new Dictionary<Note, EditorUiNote>();

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChangeEvent);
    }

    private void OnSongMetaChangeEvent(ISongMetaChangeEvent changeEvent)
    {
        ReloadSentences();
        UpdateNotesAndSentences();
    }

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
            || (voice.Name == Voice.soloVoiceName
                && settings.SongEditorSettings.HideVoices.Contains(Voice.firstVoiceName));
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
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            List<Sentence> sortedSentences = new List<Sentence>(voice.Sentences);
            sortedSentences.Sort(Sentence.comparerByStartBeat);
            voiceToSortedSentencesMap.Add(voice, sortedSentences);
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

        IEnumerable<Voice> voices = songMeta.GetVoices();

        foreach (Voice voice in voices)
        {
            if (IsVoiceVisible(voice))
            {
                DrawSentencesInVoice(voice);
            }
        }
        sentenceLinesImage.ApplyTexture();
    }

    private void DrawSentencesInVoice(Voice voice)
    {
        int viewportWidthInBeats = noteArea.MaxBeatInViewport - noteArea.MinBeatInViewport;
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];

        int sentenceIndex = 0;
        foreach (Sentence sentence in sortedSentencesOfVoice)
        {
            if (noteArea.IsInViewport(sentence))
            {
                float xStartPercent = (float)noteArea.GetHorizontalPositionForBeat(sentence.MinBeat);
                float xEndPercent = (float)noteArea.GetHorizontalPositionForBeat(sentence.ExtendedMaxBeat);
                string label = (sentenceIndex + 1).ToString();

                // Do not draw the sentence marker lines, when there are too many beats
                if (viewportWidthInBeats < 1200)
                {
                    CreateSentenceMarkerLine(xStartPercent, Colors.saddleBrown, 0);
                    CreateSentenceMarkerLine(xEndPercent, Colors.black, 20);
                }

                CreateUiSentence(sentence, xStartPercent, xEndPercent, label);
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
        List<Note> notesInLayer = songEditorLayerManager.GetNotes(layerKey)
            .Where(note => note.Sentence == null).ToList();
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
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            if (IsVoiceVisible(voice))
            {
                DrawNotesInVoice(voice);
            }
        }
    }

    private void DrawNotesInVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];
        List<Sentence> sentencesInViewport = sortedSentencesOfVoice
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

    private void CreateUiSentence(Sentence sentence, float xStartPercent, float xEndPercent, string label)
    {
        EditorUiSentence uiSentence = Instantiate(sentenceMarkerRectanglePrefab, sentenceMarkerRectangleContainer);

        injector.Inject(uiSentence);
        injector.Inject(uiSentence.GetComponent<EditorSentenceContextMenuHandler>());
        uiSentence.Init(sentence);
        uiSentence.SetText(label);

        PositionUiSentence(uiSentence.RectTransform, xStartPercent, xEndPercent);
    }

    private void PositionUiSentence(RectTransform uiSentenceRectTransform, float xStartPercent, float xEndPercent)
    {
        uiSentenceRectTransform.anchorMin = new Vector2(xStartPercent, 0);
        uiSentenceRectTransform.anchorMax = new Vector2(xEndPercent, 1);
        uiSentenceRectTransform.anchoredPosition = Vector2.zero;
        uiSentenceRectTransform.sizeDelta = Vector2.zero;
    }

    private void CreateSentenceMarkerLine(float xPercent, Color color, int yDashOffset)
    {
        if (xPercent < 0 || xPercent > 1)
        {
            return;
        }

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
