using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class EditorNoteDisplayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    public static readonly Color sentenceStartLineColor = Colors.CreateColor("#8F6A4E");
    public static readonly Color sentenceEndLineColor = Colors.CreateColor("#4F878F");

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

    private readonly Dictionary<Voice, List<Sentence>> voiceToSortedSentencesMap = new Dictionary<Voice, List<Sentence>>();

    private readonly List<ESongEditorLayer> songEditorLayerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();

    private readonly Dictionary<Note, EditorNoteControl> noteToControlMap = new Dictionary<Note, EditorNoteControl>();
    public IReadOnlyCollection<EditorNoteControl> EditorNoteControls => noteToControlMap.Values;

    private readonly Dictionary<Sentence, EditorSentenceControl> sentenceToControlMap = new Dictionary<Sentence, EditorSentenceControl>();
    public IReadOnlyCollection<EditorSentenceControl> EditorSentenceControls => sentenceToControlMap.Values;

    public void OnInjectionFinished()
    {
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
            ReloadSentences();
            UpdateNotesAndSentences();
        });

        sentenceLines.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            if (sentenceLinesDynamicTexture == null)
            {
                sentenceLinesDynamicTexture = new DynamicTexture(gameObject, sentenceLines);
                UpdateSentences();
            }
        });
    }

    private void Start()
    {
        noteAreaNotes.Clear();
        noteToControlMap.Clear();
        noteAreaSentences.Clear();
        sentenceToControlMap.Clear();

        ReloadSentences();

        UpdateNotesAndSentences();
        noteAreaControl.ViewportEventStream.Subscribe(_ =>
        {
            UpdateNotesAndSentences();
        });

        songMetaChangeEventStream
            .Subscribe(evt =>
            {
                if (evt is SentencesDeletedEvent sde)
                {
                    sde.Sentences.ForEach(sentence => DeleteSentence(sentence));
                }
            });

        foreach (ESongEditorLayer layer in EnumUtils.GetValuesAsList<ESongEditorLayer>())
        {
            songEditorLayerManager
                .ObserveEveryValueChanged(it => it.IsLayerEnabled(layer))
                .Subscribe(_ => UpdateNotes());
        }

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.HideVoices.Count)
            .Subscribe(_ => OnHideVoicesChanged())
            .AddTo(this);

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.SentenceLineSizeInDevicePixels)
            .Where(_ => sentenceLinesDynamicTexture != null)
            .Subscribe(_ => UpdateSentences());

        songEditorLayerManager.LayerChangedEventStream
            .Subscribe(_ => UpdateNotesAndSentences());
    }

    public void DeleteSentences(List<Sentence> sentences)
    {
        sentences.ForEach(sentence => DeleteSentence(sentence));
    }

    public void DeleteSentence(Sentence sentence)
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
        notVisibleNotes.ForEach(note => DeleteNoteControl(note));

        // Remove sentences of hidden voices
        List<Sentence> notVisibleSentences = sentenceToControlMap.Keys
            .Where(sentence => !songEditorLayerManager.IsVoiceVisible(sentence.Voice))
            .ToList();
        notVisibleSentences.ForEach(sentence => DeleteSentence(sentence));

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

        if (sentenceLinesDynamicTexture != null)
        {
            sentenceLinesDynamicTexture.ClearTexture();
        }

        IEnumerable<Voice> voices = songMeta.GetVoices();

        foreach (Voice voice in voices)
        {
            if (songEditorLayerManager.IsVoiceVisible(voice))
            {
                DrawSentencesInVoice(voice);
            }
        }

        if (sentenceLinesDynamicTexture != null)
        {
            sentenceLinesDynamicTexture.ApplyTexture();
        }
    }

    private void DrawSentencesInVoice(Voice voice)
    {
        int viewportWidthInBeats = noteAreaControl.MaxBeatInViewport - noteAreaControl.MinBeatInViewport;
        List<Sentence> sortedSentencesOfVoice = voiceToSortedSentencesMap[voice];

        int sentenceIndex = 0;
        foreach (Sentence sentence in sortedSentencesOfVoice)
        {
            if (noteAreaControl.IsInViewport(sentence))
            {
                float xStartPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(sentence.MinBeat);
                float xEndPercent = (float)noteAreaControl.GetHorizontalPositionForBeat(sentence.ExtendedMaxBeat);

                // Do not draw the sentence marker lines, when there are too many beats
                if (viewportWidthInBeats < 1200)
                {
                    CreateSentenceMarkerLine(xStartPercent, sentenceStartLineColor, 0);
                    CreateSentenceMarkerLine(xEndPercent, sentenceEndLineColor, 20);
                }

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
        }
    }

    public void UpdateNotes()
    {
        if (gameObject == null 
            || !gameObject.activeInHierarchy)
        {
            return;
        }
        DestroyNoteControlsOutsideOfViewport();

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

    public void DeleteNoteControl(EditorNoteControl noteControl)
    {
        noteControl.Dispose();
        noteToControlMap.Remove(noteControl.Note);
    }

    public void DeleteNoteControl(Note note)
    {
        if (noteToControlMap.TryGetValue(note, out EditorNoteControl editorNoteControl))
        {
            DeleteNoteControl(editorNoteControl);
        }
    }

    private void DestroyNoteControlsOutsideOfViewport()
    {
        ICollection<EditorNoteControl> editorNoteControls = new List<EditorNoteControl>(noteToControlMap.Values);
        foreach (EditorNoteControl editorNoteControl in editorNoteControls)
        {
            Note note = editorNoteControl.Note;
            if (!noteAreaControl.IsInViewport(note))
            {
                DeleteNoteControl(editorNoteControl);
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
                DeleteNoteControl(noteControl);
            }
        });
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
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            if (songEditorLayerManager.IsVoiceVisible(voice))
            {
                DrawNotesInVoice(voice);
            }
        }
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

    private void CreateSentenceMarkerLine(float xPercent, Color color, int yDashOffset)
    {
        if (sentenceLinesDynamicTexture == null
            || xPercent < 0
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
        visualElement.style.bottom = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        visualElement.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
    }
}
