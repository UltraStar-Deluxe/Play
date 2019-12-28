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

public class SongEditorLayerManager
{
    private readonly Dictionary<ESongEditorLayer, SongEditorLayer> layerKeyToLayerMap = CreateLayerKeyToLayerMap();

    public void AddNoteToLayer(ESongEditorLayer layer, Note note)
    {
        layerKeyToLayerMap[layer].AddNote(note);
    }

    public List<Note> GetNotes(ESongEditorLayer layerKey)
    {
        return layerKeyToLayerMap[layerKey].GetNotes();
    }

    public Color GetColor(ESongEditorLayer layerKey)
    {
        return layerKeyToLayerMap[layerKey].Color;
    }

    public bool IsLayerEnabled(ESongEditorLayer layerKey)
    {
        return layerKeyToLayerMap[layerKey].IsEnabled;
    }

    public void SetLayerEnabled(ESongEditorLayer layerKey, bool newValue)
    {
        layerKeyToLayerMap[layerKey].IsEnabled = newValue;
    }

    private static Dictionary<ESongEditorLayer, SongEditorLayer> CreateLayerKeyToLayerMap()
    {
        Dictionary<ESongEditorLayer, SongEditorLayer> result = new Dictionary<ESongEditorLayer, SongEditorLayer>();
        List<ESongEditorLayer> layerKeys = EnumUtils.GetValuesAsList<ESongEditorLayer>();
        foreach (ESongEditorLayer layerKey in layerKeys)
        {
            result.Add(layerKey, new SongEditorLayer(layerKey));
        }
        result[ESongEditorLayer.MicRecording].Color = Colors.coral;
        result[ESongEditorLayer.ButtonRecording].Color = Colors.indigo;
        return result;
    }

    public List<Note> GetAllNotes()
    {
        List<Note> notes = new List<Note>();
        foreach (ESongEditorLayer layerKey in layerKeyToLayerMap.Keys)
        {
            List<Note> notesOfLayer = GetNotes(layerKey);
            notes.AddRange(notesOfLayer);
        }
        return notes;
    }
}
