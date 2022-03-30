﻿using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class EditorNoteDisplayer : MonoBehaviour, INeedInjection
{
    public static readonly Color sentenceStartLineColor = Colors.CreateColor("#8F6A4E");
    public static readonly Color sentenceEndLineColor = Colors.CreateColor("#4F878F");

    private const int HideElementThresholdInMillis = 60 * 1000;

    [InjectedInInspector]
    public VisualTreeAsset editorNoteUi;

    [InjectedInInspector]
    public VisualTreeAsset editorSentenceUi;

    [Inject(UxmlName = R.UxmlNames.noteAreaNotes)]
    private VisualElement noteAreaNotes;

    [Inject(UxmlName = R.UxmlNames.sentenceLines)]
    private VisualElement sentenceLines;

    [Inject(UxmlName = R.UxmlNames.noteAreaSentences)]
    public VisualElement noteAreaSentences;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    private DynamicTexture sentenceLinesDynamicTexture;

    private readonly Dictionary<Voice, List<Sentence>> voiceToSortedSentencesMap = new();

    private readonly List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorNoteControl> noteToControlMap = new();
    public IReadOnlyCollection<EditorNoteControl> EditorNoteControls => noteToControlMap.Values;

    private readonly Dictionary<Sentence, EditorSentenceControl> sentenceToControlMap = new();
    public IReadOnlyCollection<EditorSentenceControl> EditorSentenceControls => sentenceToControlMap.Values;

    private int lastFullUpdateFrame;

    private int lastViewportWidthInMillis;

    private void Start()
    {
        noteAreaNotes.Clear();
        noteToControlMap.Clear();
        noteAreaSentences.Clear();
        sentenceToControlMap.Clear();

        ReloadSentences();

        UpdateNotesAndSentences();
        noteAreaControl.ViewportEventStream
            .Subscribe(_ =>
            {
                UpdateNotesAndSentences();
                lastViewportWidthInMillis = noteAreaControl.ViewportWidth;
            }).AddTo(gameObject);

        songMetaChangeEventStream.Subscribe(evt =>
        {
            if (evt is LoadedMementoEvent)
            {
                // The object instances have changed. All maps must be cleared.
                noteAreaNotes.Clear();
                noteToControlMap.Clear();
                noteAreaSentences.Clear();
                sentenceToControlMap.Clear();
            }
            else if (evt is SentencesDeletedEvent sde)
            {
                sde.Sentences.ForEach(sentence => RemoveSentence(sentence));
            }
            else if (evt is MovedNotesToVoiceEvent movedNotesToVoiceEvent)
            {
                // Sentences might have been removed because they did not contain any notes anymore.
                RemoveSentences(movedNotesToVoiceEvent.RemovedSentences);
            }

            ReloadSentences();
            UpdateNotesAndSentences();
        }).AddTo(gameObject);

        sentenceLines.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            if (sentenceLinesDynamicTexture == null)
            {
                sentenceLinesDynamicTexture = new DynamicTexture(gameObject, sentenceLines);
                UpdateSentences();
            }
        });

        EnumUtils.GetValuesAsList<ESongEditorLayer>().ForEach(layer =>
        {
            songEditorLayerManager
                .ObserveEveryValueChanged(it => it.IsLayerEnabled(layer))
                .Subscribe(_ => UpdateNotes())
                .AddTo(gameObject);
        });

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.HideVoices.Count)
            .Subscribe(_ => OnHideVoicesChanged())
            .AddTo(gameObject);

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.SentenceLineSizeInDevicePixels)
            .Subscribe(newValue =>
            {
                if (newValue <= 0)
                {
                    RemoveSentenceMarkerLines();
                }
                else
                {
                    UpdateSentenceMarkerLines();
                }
            })
            .AddTo(gameObject);

        songEditorLayerManager.LayerChangedEventStream
            .Subscribe(_ => UpdateNotesAndSentences())
            .AddTo(gameObject);
    }

    public void RemoveSentences(IReadOnlyCollection<Sentence> sentences)
    {
        sentences.ForEach(sentence => RemoveSentence(sentence));
    }

    public void RemoveSentence(Sentence sentence)
    {
        if (sentenceToControlMap.TryGetValue(sentence, out EditorSentenceControl editorSentenceControl))
        {
            editorSentenceControl.Dispose();
            sentenceToControlMap.Remove(sentence);
        }
    }

    private void OnHideVoicesChanged()
    {
        // Remove notes of hidden voices
        List<Note> notVisibleNotes = noteToControlMap.Keys
            .Where(note => !songEditorLayerManager.IsVoiceVisible(note.Sentence?.Voice))
            .ToList();
        notVisibleNotes.ForEach(note => RemoveNoteControl(note));

        // Remove sentences of hidden voices
        List<Sentence> notVisibleSentences = sentenceToControlMap.Keys
            .Where(sentence => !songEditorLayerManager.IsVoiceVisible(sentence.Voice))
            .ToList();
        notVisibleSentences.ForEach(sentence => RemoveSentence(sentence));

        // Draw any notes that are now (again) visible.
        UpdateNotesAndSentences();
    }

    public void ClearNoteControls()
    {
        noteAreaNotes.Clear();
        noteToControlMap.Values.ForEach(editorNoteControl => editorNoteControl.Dispose());
        noteToControlMap.Clear();
    }

    public void ReloadSentences()
    {
        voiceToSortedSentencesMap.Clear();
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            List<Sentence> sortedSentences = new(voice.Sentences);
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

        UpdateSentenceControls();
        UpdateSentenceMarkerLines();
    }

    private void UpdateSentenceControls()
    {
        List<Voice> visibleVoices = songMeta.GetVoices()
            .Where(voice => songEditorLayerManager.IsVoiceVisible(voice))
            .ToList();

        visibleVoices.ForEach(voice => CreateSentenceControlForVoice(voice));
    }

    private void RemoveSentenceMarkerLines()
    {
        if (sentenceLinesDynamicTexture == null)
        {
            return;
        }

        sentenceLinesDynamicTexture.ClearTexture();
        sentenceLinesDynamicTexture.ApplyTexture();
    }

    private void UpdateSentenceMarkerLines()
    {
        if (settings.SongEditorSettings.SentenceLineSizeInDevicePixels <= 0
            || sentenceLinesDynamicTexture == null)
        {
            return;
        }

        if (noteAreaControl.ViewportWidth > HideElementThresholdInMillis)
        {
            if (lastViewportWidthInMillis <= HideElementThresholdInMillis)
            {
                RemoveSentenceMarkerLines();
                lastViewportWidthInMillis = noteAreaControl.ViewportWidth;
            }
            return;
        }

        List<Voice> visibleVoices = songMeta.GetVoices()
            .Where(voice => songEditorLayerManager.IsVoiceVisible(voice))
            .ToList();

        sentenceLinesDynamicTexture.ClearTexture();
        visibleVoices.ForEach(voice => DrawSentenceMarkerLineForVoice(voice));
        sentenceLinesDynamicTexture.ApplyTexture();
    }

    private void CreateSentenceControlForVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];
        int sentenceIndex = 0;
        sortedSentencesOfVoice.ForEach(sentence =>
        {
            if (noteAreaControl.IsInViewport(sentence))
            {
                float xStartPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(sentence.MinBeat);
                float xEndPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(sentence.ExtendedMaxBeat);

                UpdateOrCreateSentenceControl(sentence, xStartPercent, xEndPercent, sentenceIndex);
            }
            else
            {
                if (sentenceToControlMap.TryGetValue(sentence, out EditorSentenceControl editorSentenceControl))
                {
                    editorSentenceControl.Dispose();
                    sentenceToControlMap.Remove(sentence);
                }
            }
            sentenceIndex++;
        });
    }

    public void UpdateNotes()
    {
        if (gameObject == null
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        HideNoteControlsOutsideOfViewport();

        DrawNotesInSongFile();
        DrawNotesInLayers();
    }

    public EditorNoteControl GetNoteControl(Note note)
    {
        if (noteToControlMap.TryGetValue(note, out EditorNoteControl editorNoteControl))
        {
            return editorNoteControl;
        }
        else
        {
            return null;
        }
    }

    public void RemoveNoteControl(EditorNoteControl noteControl)
    {
        noteControl.Dispose();
        noteToControlMap.Remove(noteControl.Note);
    }

    public void RemoveNoteControl(Note note)
    {
        if (noteToControlMap.TryGetValue(note, out EditorNoteControl editorNoteControl))
        {
            RemoveNoteControl(editorNoteControl);
        }
    }

    private void HideNoteControlsOutsideOfViewport()
    {
        ICollection<EditorNoteControl> editorNoteControls = new List<EditorNoteControl>(noteToControlMap.Values);
        foreach (EditorNoteControl editorNoteControl in editorNoteControls)
        {
            Note note = editorNoteControl.Note;
            if (noteAreaControl.IsInViewport(note))
            {
                continue;
            }

            HideNoteControl(editorNoteControl);
        }
    }

    private void HideNoteControl(EditorNoteControl editorNoteControl)
    {
        editorNoteControl.VisualElement.HideByDisplay();
    }

    private void ShowNoteControl(EditorNoteControl editorNoteControl)
    {
        editorNoteControl.VisualElement.ShowByDisplay();
    }

    private void DrawNotesInLayers()
    {
        foreach (ESongEditorLayer layerKey in songEditorLayerKeys)
        {
            if (songEditorLayerManager.IsLayerEnabled(layerKey))
            {
                DrawNotesInLayer(layerKey);
            }
            else
            {
                ClearNotesInLayer(layerKey);
            }
        }
    }

    public void ClearNotesInLayer(ESongEditorLayer layerKey)
    {
        List<Note> notesInLayer = songEditorLayerManager.GetNotes(layerKey)
            .Where(note => note.Sentence == null).ToList();
        notesInLayer.ForEach(note =>
        {
            if (noteToControlMap.TryGetValue(note, out EditorNoteControl noteControl))
            {
                RemoveNoteControl(noteControl);
            }
        });
    }

    public bool AnyNoteControlContainsPosition(Vector2 pos)
    {
        return EditorNoteControls.AnyMatch(editorNoteControl =>
            editorNoteControl.VisualElement.worldBound.Contains(pos));
    }

    public bool AnySentenceControlContainsPosition(Vector2 pos)
    {
        return EditorSentenceControls.AnyMatch(editorSentenceControl =>
            editorSentenceControl.VisualElement.worldBound.Contains(pos));
    }

    private void DrawNotesInLayer(ESongEditorLayer layerKey)
    {
        List<Note> notesInLayer = songEditorLayerManager.GetNotes(layerKey)
            .Where(note => note.Sentence == null).ToList();
        List<Note> notesInViewport = notesInLayer
            .Where(note => noteAreaControl.IsInViewport(note))
            .ToList();

        Color layerColor = songEditorLayerManager.GetColor(layerKey);
        foreach (Note note in notesInViewport)
        {
            EditorNoteControl noteControl = UpdateOrCreateNoteControl(note);
            if (noteControl != null)
            {
                noteControl.SetColor(layerColor);
            }
        }
    }

    private void DrawNotesInSongFile()
    {
        IEnumerable<Voice> visibleVoices = songMeta.GetVoices()
            .Where(voice => songEditorLayerManager.IsVoiceVisible(voice))
            .ToList();
        visibleVoices.ForEach(voice => DrawNotesInVoice(voice));
    }

    private void DrawNotesInVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];
        List<Sentence> sentencesInViewport = sortedSentencesOfVoice
            .Where(sentence => noteAreaControl.IsInViewport(sentence))
            .ToList();

        List<Note> notesInViewport = sentencesInViewport
                .SelectMany(sentence => sentence.Notes)
                .Where(note => noteAreaControl.IsInViewport(note))
                .ToList();

        foreach (Note note in notesInViewport)
        {
            UpdateOrCreateNoteControl(note);
        }
    }

    private void UpdateOrCreateSentenceControl(Sentence sentence, float xStartPercent, float xEndPercent, int sentenceIndex)
    {
        if (!sentenceToControlMap.TryGetValue(sentence, out EditorSentenceControl editorSentenceControl))
        {
            VisualElement sentenceVisualElement = editorSentenceUi.CloneTree().Children().First();
            editorSentenceControl = injector
                .WithRootVisualElement(sentenceVisualElement)
                .WithBindingForInstance(sentence)
                .CreateAndInject<EditorSentenceControl>();
            sentenceToControlMap[sentence] = editorSentenceControl;
            noteAreaSentences.Add(sentenceVisualElement);
        }

        string label = (sentenceIndex + 1).ToString();
        editorSentenceControl.SetText(label);

        // Update color
        if (sentence.Voice != null)
        {
            Color color = songEditorSceneControl.GetColorForVoice(sentence.Voice);
            editorSentenceControl.SetColor(color);

            // Make sentence rectangles alternating light/dark
            bool isDark = (sentenceIndex % 2) == 0;
            if (isDark)
            {
                Color darkColor = songEditorSceneControl.GetColorForVoice(sentence.Voice).Multiply(0.66f);
                editorSentenceControl.SetColor(darkColor);
            }
        }

        PositionSentenceControl(editorSentenceControl.VisualElement, xStartPercent, xEndPercent);
    }

    private void PositionSentenceControl(VisualElement visualElement, float xStartPercent, float xEndPercent)
    {
        float widthPercent = xEndPercent - xStartPercent;
        visualElement.style.left = new StyleLength(new Length(xStartPercent * 100, LengthUnit.Percent));
        visualElement.style.bottom = 0;
        visualElement.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
    }

    private void DrawSentenceMarkerLineForVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];
        sortedSentencesOfVoice.ForEach(sentence =>
        {
            float xStartPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(sentence.MinBeat);
            float xEndPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(sentence.ExtendedMaxBeat);

            DrawSentenceMarkerLine(xStartPercent, sentenceStartLineColor, 0);
            DrawSentenceMarkerLine(xEndPercent, sentenceEndLineColor, 20);
        });
    }

    private void DrawSentenceMarkerLine(float xPercent, Color color, int yDashOffset)
    {
        if (xPercent < 0
            || xPercent > 1)
        {
            return;
        }

        int width = settings.SongEditorSettings.SentenceLineSizeInDevicePixels;
        int xFrom = (int)(xPercent * sentenceLinesDynamicTexture.TextureWidth);
        int xTo = xFrom + width;

        for (int x = xFrom; x < xTo && x < sentenceLinesDynamicTexture.TextureWidth; x++)
        {
            for (int y = 0; y < sentenceLinesDynamicTexture.TextureHeight; y++)
            {
                // Make it dashed
                if (((y + yDashOffset) % 40) < 20)
                {
                    sentenceLinesDynamicTexture.SetPixel(x, y, color);
                }
            }
        }
    }

    private EditorNoteControl UpdateOrCreateNoteControl(Note note)
    {
        if (!noteToControlMap.TryGetValue(note, out EditorNoteControl editorNoteControl))
        {
            VisualElement noteVisualElement = editorNoteUi.CloneTree().Children().First();
            editorNoteControl = injector
                .WithRootVisualElement(noteVisualElement)
                .WithBindingForInstance(note)
                .CreateAndInject<EditorNoteControl>();
            noteToControlMap.Add(note, editorNoteControl);
            noteAreaNotes.Add(noteVisualElement);
        }
        else
        {
            editorNoteControl.SyncWithNote();
        }

        PositionNoteControl(editorNoteControl.VisualElement, note.MidiNote, note.StartBeat, note.EndBeat);
        ShowNoteControl(editorNoteControl);

        if (noteAreaControl.ViewportWidth > HideElementThresholdInMillis)
        {
            editorNoteControl.HideLabels();
        }
        else
        {
            editorNoteControl.ShowLabels();
        }

        return editorNoteControl;
    }

    private void PositionNoteControl(VisualElement visualElement, int midiNote, int startBeat, int endBeat)
    {
        float heightPercent = noteAreaControl.HeightForSingleNote;
        float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(midiNote) - heightPercent / 2;
        float xStartPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(startBeat);
        float xEndPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(endBeat);
        float widthPercent = xEndPercent - xStartPercent;

        visualElement.style.left = new StyleLength(new Length(xStartPercent * 100, LengthUnit.Percent));
        visualElement.style.top = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        visualElement.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
    }
}
