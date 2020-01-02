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

public class SongEditorHistoryManager : MonoBehaviour, INeedInjection
{
    private static readonly int maxHistoryLength = 3;

    private int indexInHistory = -1;
    private readonly List<SongEditorMemento> history = new List<SongEditorMemento>();

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

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

    void Start()
    {
        AddUndoState();
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

    private void LoadUndoState(SongEditorMemento undoState)
    {
        LoadLayers(undoState);
        LoadVoices(undoState);

        editorNoteDisplayer.ClearUiNotes();
        editorNoteDisplayer.ReloadSentences();
        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    private void LoadVoices(SongEditorMemento undoState)
    {
        foreach (Voice voice in undoState.Voices)
        {
            Voice matchingVoice = songMeta.GetVoices()
                .Where(it => it.Name == voice.Name).FirstOrDefault();
            if (matchingVoice != null)
            {
                List<Sentence> sentencesCopy = new List<Sentence>();
                foreach (Sentence sentence in voice.Sentences)
                {
                    Sentence sentenceCopy = sentence.CloneDeep();
                    sentencesCopy.Add(sentenceCopy);
                }
                matchingVoice.SetSentences(sentencesCopy);
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
