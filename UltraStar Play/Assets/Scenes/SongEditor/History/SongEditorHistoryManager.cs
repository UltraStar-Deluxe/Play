using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorHistoryManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    private static readonly int maxHistoryLength = 20;

    // Static reference to last state to load it when opening the song editor scene
    // (e.g. after switching editor > sing > editor).
    private static Dictionary<SongMeta, SongEditorMemento> songMetaToSongEditorMementoMap = new Dictionary<SongMeta, SongEditorMemento>();

    private int indexInHistory = -1;
    private readonly List<SongEditorMemento> history = new List<SongEditorMemento>();

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
        songMetaChangeEventStream.Subscribe(OnSongMetaChangeEvent);
    }

    private void OnSongMetaChangeEvent(ISongMetaChangeEvent changeEvent)
    {
        if (!(changeEvent is LoadedMementoEvent))
        {
            AddUndoState();
        }
    }

    void Start()
    {
        // Restore the last state of the editor for this song.
        if (songMetaToSongEditorMementoMap.TryGetValue(songMeta, out SongEditorMemento memento))
        {
            LoadUndoState(memento);
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
        LoadUndoState(undoState);
    }

    public void Redo()
    {
        if (indexInHistory >= history.Count - 1)
        {
            return;
        }

        indexInHistory++;
        SongEditorMemento undoState = history[indexInHistory];
        LoadUndoState(undoState);
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
        SongEditorMemento memento = new SongEditorMemento();
        SaveVoices(memento);
        SaveLayers(memento);
        SaveSongMetaTags(memento);
        return memento;
    }

    private void SaveLayers(SongEditorMemento memento)
    {
        List<SongEditorLayer> layers = layerManager.GetLayers();
        foreach (SongEditorLayer layer in layers)
        {
            SongEditorLayer layerCopy = layer.CloneDeep();
            memento.Layers.Add(layerCopy);
        }
    }

    private void SaveVoices(SongEditorMemento memento)
    {
        IEnumerable<Voice> voices = songMeta.GetVoices();
        foreach (Voice voice in voices)
        {
            Voice voiceCopy = voice.CloneDeep();
            memento.Voices.Add(voiceCopy);
        }
    }

    private void SaveSongMetaTags(SongEditorMemento memento)
    {
        memento.Bpm = songMeta.Bpm;
        memento.MusicGap = songMeta.Gap;
    }

    private void LoadUndoState(SongEditorMemento undoState)
    {
        LoadLayers(undoState);
        LoadVoices(undoState);
        LoadSongMetaTags(undoState);

        editorNoteDisplayer.ClearUiNotes();

        songMetaChangeEventStream.OnNext(new LoadedMementoEvent());

        songMetaToSongEditorMementoMap[songMeta] = undoState;
    }

    private void LoadSongMetaTags(SongEditorMemento memento)
    {
        songMeta.Bpm = memento.Bpm;
        songMeta.Gap = memento.MusicGap;
    }

    private void LoadVoices(SongEditorMemento undoState)
    {
        IReadOnlyCollection<Voice> voicesInSongMeta = songMeta.GetVoices();
        // Add / update voices from memento
        foreach (Voice voiceMemento in undoState.Voices)
        {
            Voice matchingVoiceInSongMeta = voicesInSongMeta
                .Where(it => it.Name == voiceMemento.Name).FirstOrDefault();
            if (matchingVoiceInSongMeta == null)
            {
                // Create new voice
                Voice voiceMementoClone = voiceMemento.CloneDeep();
                songMeta.AddVoice(voiceMementoClone);
            }
            else
            {
                // Update existing voice
                List<Sentence> sentencesCopy = new List<Sentence>();
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
                .Where(it => it.Name == voiceInSongMeta.Name).FirstOrDefault();
            if (matchingVoiceMemento == null)
            {
                songMeta.RemoveVoice(voiceInSongMeta);
            }
        }
    }

    private void LoadLayers(SongEditorMemento undoState)
    {
        foreach (SongEditorLayer layer in undoState.Layers)
        {
            layerManager.ClearLayer(layer.LayerKey);
            foreach (Note note in layer.GetNotes())
            {
                layerManager.AddNoteToLayer(layer.LayerKey, note);
            }
        }
    }
}
