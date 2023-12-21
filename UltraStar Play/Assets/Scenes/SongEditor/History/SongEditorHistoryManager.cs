using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorHistoryManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        songMetaToSongEditorMementoMap.Clear();
    }

    private static readonly int maxHistoryLength = 30;

    // Static reference to last state to load it when opening the song editor scene
    // (e.g. after switching editor > sing > editor).
    private static readonly Dictionary<SongMeta, SongEditorMemento> songMetaToSongEditorMementoMap = new();

    private int indexInHistory = -1;
    private readonly List<SongEditorMemento> history = new();

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream
            .Where(evt => IsUndoable(evt))
            // When there is no new change to the song for some time, then record an undo-state.
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => AddUndoState())
            .AddTo(gameObject);
    }

    private void Start()
    {
        // Restore the last state of the editor for this song.
        if (songMetaToSongEditorMementoMap.TryGetValue(songMeta, out SongEditorMemento memento))
        {
            // Do not load voices. The song may have been reloaded or sing-along data newly created in the meantime.
            LoadUndoState(memento, false);
        }

        AddUndoState();
    }

    public void Undo()
    {
        if (indexInHistory <= 0)
        {
            return;
        }

        indexInHistory--;
        SongEditorMemento undoState = history[indexInHistory];
        LoadUndoState(undoState, true);
    }

    public void Redo()
    {
        if (indexInHistory >= history.Count - 1)
        {
            return;
        }

        indexInHistory++;
        SongEditorMemento undoState = history[indexInHistory];
        LoadUndoState(undoState, true);
    }

    public void AddUndoState()
    {
        // Discard undone history
        int discardedHistoryCount = history.Count - (indexInHistory + 1);
        for (int i = 0; i < discardedHistoryCount; i++)
        {
            RemoveNewestState();
        }

        SongEditorMemento undoState = CreateUndoState();
        songMetaToSongEditorMementoMap[songMeta] = undoState;
        history.Add(undoState);
        for (int i = history.Count - 1; i > maxHistoryLength; i--)
        {
            RemoveOldestState();
        }
        indexInHistory = history.Count - 1;
    }

    private void RemoveNewestState()
    {
        if (history.Count > 0)
        {
            history.RemoveAt(history.Count - 1);
        }
    }

    private void RemoveOldestState()
    {
        if (history.Count > 0)
        {
            history.RemoveAt(0);
        }
    }

    private SongEditorMemento CreateUndoState()
    {
        SongEditorMemento memento = new();
        SaveVoices(memento);
        SaveLayers(memento);
        SaveSongMetaTags(memento);
        return memento;
    }

    private void SaveLayers(SongEditorMemento memento)
    {
        List<SongEditorEnumLayer> layers = layerManager.GetEnumLayers();
        foreach (SongEditorEnumLayer layer in layers)
        {
            SongEditorEnumLayer enumLayerCopy = layer.CloneDeep();
            memento.Layers.Add(enumLayerCopy);
        }
    }

    private void SaveVoices(SongEditorMemento memento)
    {
        IEnumerable<Voice> voices = songMeta.Voices;
        foreach (Voice voice in voices)
        {
            Voice voiceCopy = voice.CloneDeep();
            memento.Voices.Add(voiceCopy);
        }
    }

    private void SaveSongMetaTags(SongEditorMemento memento)
    {
        memento.BeatsPerMinute = songMeta.BeatsPerMinute;
        memento.GapInMillis = songMeta.GapInMillis;
    }

    private void LoadUndoState(SongEditorMemento undoState, bool loadVoices)
    {
        LoadLayers(undoState);
        if (loadVoices)
        {
            LoadVoices(undoState);
        }
        LoadSongMetaTags(undoState);

        editorNoteDisplayer.ClearNoteControls();

        songMetaChangeEventStream.OnNext(new LoadedMementoEvent());

        songMetaToSongEditorMementoMap[songMeta] = undoState;
    }

    private void LoadSongMetaTags(SongEditorMemento memento)
    {
        songMeta.BeatsPerMinute = memento.BeatsPerMinute;
        songMeta.GapInMillis = memento.GapInMillis;
    }

    private void LoadVoices(SongEditorMemento undoState)
    {
        IReadOnlyCollection<Voice> voicesInSongMeta = songMeta.Voices;
        // Add / update voices from memento
        foreach (Voice voiceMemento in undoState.Voices)
        {
            Voice matchingVoiceInSongMeta = voicesInSongMeta
                .FirstOrDefault(voice => voice.Id == voiceMemento.Id);
            if (matchingVoiceInSongMeta == null)
            {
                // Create new voice
                Voice voiceMementoClone = voiceMemento.CloneDeep();
                SongMetaUtils.AddVoice(songMeta, voiceMementoClone);
            }
            else
            {
                // Update existing voice
                List<Sentence> sentencesCopy = new();
                foreach (Sentence sentence in voiceMemento.Sentences)
                {
                    Sentence sentenceCopy = sentence.CloneDeep();
                    sentencesCopy.Add(sentenceCopy);
                }
                matchingVoiceInSongMeta.SetSentences(sentencesCopy);
            }
        }

        // Remove voices that do not exist in memento
        foreach (Voice voiceInSongMeta in new List<Voice>(voicesInSongMeta))
        {
            Voice matchingVoiceMemento = undoState.Voices
                .FirstOrDefault(voice => voice.Id == voiceInSongMeta.Id);
            if (matchingVoiceMemento == null)
            {
                SongMetaUtils.RemoveVoice(songMeta, voiceInSongMeta);
            }
        }
    }

    private void LoadLayers(SongEditorMemento undoState)
    {
        foreach (SongEditorEnumLayer layer in undoState.Layers)
        {
            layerManager.ClearEnumLayer(layer.LayerEnum);
            foreach (Note note in layer.GetNotes())
            {
                layerManager.AddNoteToEnumLayer(layer.LayerEnum, note);
            }
        }
    }

    private bool IsUndoable(SongMetaChangeEvent evt)
    {
        return evt.Undoable
               && evt is not LoadedMementoEvent;
    }
}
