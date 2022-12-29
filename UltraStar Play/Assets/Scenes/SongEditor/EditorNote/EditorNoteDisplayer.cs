using System.Collections.Generic;
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

    private const int HideElementThresholdInMillis = 40 * 1000;

    [InjectedInInspector]
    public VisualTreeAsset editorNoteUi;

    [InjectedInInspector]
    public VisualTreeAsset editorSentenceUi;

    [Inject(UxmlName = R.UxmlNames.noteAreaNotes)]
    private VisualElement noteAreaNotes;

    [Inject(UxmlName = R.UxmlNames.noteAreaNotesBackground)]
    private VisualElement noteAreaNotesBackground;

    [Inject(UxmlName = R.UxmlNames.noteAreaNotesForeground)]
    private VisualElement noteAreaNotesForeground;

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

    private readonly Dictionary<Note, EditorNoteControl> noteToControlMap = new();
    public IReadOnlyCollection<EditorNoteControl> EditorNoteControls => noteToControlMap.Values;

    private readonly Dictionary<Sentence, EditorSentenceControl> sentenceToControlMap = new();
    public IReadOnlyCollection<EditorSentenceControl> EditorSentenceControls => sentenceToControlMap.Values;

    private int lastFullUpdateFrame;

    private int lastViewportWidthInMillis;

    private readonly Dictionary<ESongEditorLayer, VisualElement> songEditorLayerToParentElement = new();

    private void Start()
    {
        noteAreaNotesBackground.Clear();
        noteAreaNotes.Clear();
        noteAreaNotesForeground.Clear();
        noteToControlMap.Clear();
        noteAreaSentences.Clear();
        sentenceToControlMap.Clear();

        ReloadSentences();

        songEditorLayerToParentElement.Add(ESongEditorLayer.ButtonRecording, noteAreaNotesBackground);
        songEditorLayerToParentElement.Add(ESongEditorLayer.MicRecording, noteAreaNotesBackground);
        songEditorLayerToParentElement.Add(ESongEditorLayer.MidiFile, noteAreaNotesBackground);
        songEditorLayerToParentElement.Add(ESongEditorLayer.CopyPaste, noteAreaNotesForeground);

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
                noteAreaNotesBackground.Clear();
                noteAreaNotes.Clear();
                noteAreaNotesForeground.Clear();
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
                .ObserveEveryValueChanged(it => it.IsEnumLayerVisible(layer))
                .Subscribe(_ => UpdateNotes())
                .AddTo(gameObject);
        });

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

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.ShowNotePitchLabel)
            .Subscribe(_ => UpdateNotesAndSentences())
            .AddTo(gameObject);

        songEditorLayerManager.LayerChangedEventStream
            .Subscribe(evt =>
            {
                if (evt.IsVoiceLayerEvent)
                {
                    OnVoiceLayerChanged();
                }
                else
                {
                    UpdateNotesAndSentences();
                }
            })
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

    private void OnVoiceLayerChanged()
    {
        // Remove notes of hidden voices
        List<Note> notVisibleNotes = noteToControlMap.Keys
            .Where(note => !songEditorLayerManager.IsVoiceLayerVisible(note.Sentence?.Voice.Name))
            .ToList();
        notVisibleNotes.ForEach(note => RemoveNoteControl(note));

        // Remove sentences of hidden voices
        List<Sentence> notVisibleSentences = sentenceToControlMap.Keys
            .Where(sentence => !songEditorLayerManager.IsVoiceLayerVisible(sentence.Voice.Name))
            .ToList();
        notVisibleSentences.ForEach(sentence => RemoveSentence(sentence));

        // Draw any notes that are now (again) visible.
        UpdateNotesAndSentences();
    }

    public void ClearNoteControls()
    {
        noteAreaNotesBackground.Clear();
        noteAreaNotes.Clear();
        noteAreaNotesForeground.Clear();
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
            .Where(voice => songEditorLayerManager.IsVoiceLayerVisible(voice.Name))
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
            .Where(voice => songEditorLayerManager.IsVoiceLayerVisible(voice.Name))
            .ToList();

        sentenceLinesDynamicTexture.ClearTexture();
        visibleVoices.ForEach(voice => DrawSentenceMarkerLineForVoice(voice));
        sentenceLinesDynamicTexture.ApplyTexture();
    }

    private List<Sentence> GetSortedSentencesForVoice(Voice voice)
    {
        if (voiceToSortedSentencesMap.TryGetValue(voice, out List<Sentence> sortedSentences))
        {
            return sortedSentences;
        }

        return new();
    }

    private void CreateSentenceControlForVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = GetSortedSentencesForVoice(voice);
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

        DrawNotesInLayers(EnumUtils.GetValuesAsList<ESongEditorLayer>());
        DrawNotesInSongFile();
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

    private void DrawNotesInLayers(List<ESongEditorLayer> layerEnums)
    {
        foreach (ESongEditorLayer layerKey in layerEnums)
        {
            if (songEditorLayerManager.IsEnumLayerVisible(layerKey))
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
        List<Note> notesInLayer = songEditorLayerManager.GetEnumLayerNotes(layerKey)
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

    private void DrawNotesInLayer(ESongEditorLayer layerEnum)
    {
        IEnumerable<Note> notesInLayer = songEditorLayerManager.GetEnumLayerNotes(layerEnum)
            .Where(note => note.Sentence == null);
        IEnumerable<Note> notesInViewport = notesInLayer
            .Where(note => noteAreaControl.IsInViewport(note));

        Color layerColor = songEditorLayerManager.GetEnumLayerColor(layerEnum);
        SongEditorEnumLayer layer = songEditorLayerManager.GetEnumLayer(layerEnum);
        foreach (Note note in notesInViewport)
        {
            EditorNoteControl noteControl = UpdateOrCreateNoteControl(note, layer);
            if (noteControl != null)
            {
                noteControl.SetColor(layerColor);
            }
        }
    }

    private void DrawNotesInSongFile()
    {
        IEnumerable<Voice> visibleVoices = songMeta.GetVoices()
            .Where(voice => songEditorLayerManager.IsVoiceLayerVisible(voice.Name))
            .ToList();
        visibleVoices.ForEach(voice => DrawNotesInVoice(voice));
    }

    private void DrawNotesInVoice(Voice voice)
    {
        List<Sentence> sortedSentencesOfVoice = GetSortedSentencesForVoice(voice);
        List<Sentence> sentencesInViewport = sortedSentencesOfVoice
            .Where(sentence => noteAreaControl.IsInViewport(sentence))
            .ToList();

        List<Note> notesInViewport = sentencesInViewport
                .SelectMany(sentence => sentence.Notes)
                .Where(note => noteAreaControl.IsInViewport(note))
                .ToList();

        foreach (Note note in notesInViewport)
        {
            UpdateOrCreateNoteControl(note, null);
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
            Color color = songEditorLayerManager.GetVoiceLayerColor(sentence.Voice.Name);
            editorSentenceControl.SetColor(color);

            // Make sentence rectangles alternating light/dark
            bool isDark = (sentenceIndex % 2) == 0;
            if (isDark)
            {
                Color darkColor = songEditorLayerManager.GetVoiceLayerColor(sentence.Voice.Name).Multiply(0.66f);
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
        List<Sentence> sortedSentencesOfVoice = GetSortedSentencesForVoice(voice);
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

    private EditorNoteControl UpdateOrCreateNoteControl(Note note, SongEditorEnumLayer layer)
    {
        if (!noteToControlMap.TryGetValue(note, out EditorNoteControl editorNoteControl))
        {
            DisposableStopwatch stopwatchA = new("");
            VisualElement noteVisualElement = editorNoteUi.CloneTree().Children().First();
            editorNoteControl = injector
                .WithRootVisualElement(noteVisualElement)
                .WithBindingForInstance(note)
                .CreateAndInject<EditorNoteControl>();
            noteToControlMap.Add(note, editorNoteControl);
            VisualElement parentElement = layer != null
                ? songEditorLayerToParentElement[layer.LayerEnum]
                : noteAreaNotes;
            parentElement.Add(noteVisualElement);
        }
        else
        {
            editorNoteControl.SyncWithNote();
        }

        float heightFactor = layer != null
            ? 0.66f
            : 1f;
        PositionNoteControl(editorNoteControl.VisualElement, note.MidiNote, note.StartBeat, note.EndBeat, heightFactor);
        ShowNoteControl(editorNoteControl);

        if (noteAreaControl.ViewportWidth < HideElementThresholdInMillis)
        {
            if (settings.SongEditorSettings.ShowNotePitchLabel)
            {
                editorNoteControl.ShowPitchLabel();
            }
            else
            {
                editorNoteControl.HidePitchLabel();
            }
            editorNoteControl.ShowLyricsLabel();
        }
        else
        {
            editorNoteControl.HidePitchLabel();
            editorNoteControl.HideLyricsLabel();
        }

        if (note.IsEditable)
        {
            editorNoteControl.VisualElement.pickingMode = PickingMode.Position;
        }
        else
        {
            editorNoteControl.VisualElement.pickingMode = PickingMode.Ignore;
        }

        return editorNoteControl;
    }

    private void PositionNoteControl(VisualElement visualElement, int midiNote, int startBeat, int endBeat, float heightFactor)
    {
        float heightPercent = noteAreaControl.HeightForSingleNote * heightFactor;
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
