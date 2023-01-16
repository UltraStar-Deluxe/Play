using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorLayerManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    private readonly Dictionary<ESongEditorLayer, SongEditorEnumLayer> layerEnumToLayerMap = CreateLayerEnumToLayerMap();
    private readonly Dictionary<string, SongEditorVoiceLayer> voiceNameToLayerMap = CreateVoiceNameToLayerMap();

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    private readonly Subject<LayerChangedEvent> layerChangedEventStream = new();
    public IObservable<LayerChangedEvent> LayerChangedEventStream => layerChangedEventStream;

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (changeEvent is MovedNotesToVoiceEvent mntve)
        {
            mntve.Notes
                .Where(note => note.Sentence != null)
                .ForEach(note => RemoveNoteFromAllEnumLayers(note));
        }
    }
    
    public void AddNoteToEnumLayer(ESongEditorLayer layerEnum, Note note)
    {
        layerEnumToLayerMap[layerEnum].AddNote(note);
    }

    public void ClearEnumLayer(ESongEditorLayer layerEnum)
    {
        layerEnumToLayerMap[layerEnum].ClearNotes();
    }

    public List<Note> GetEnumLayerNotes(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].GetNotes();
    }

    public List<Note> GetVoiceLayerNotes(string voiceName)
    {
        voiceName = Voice.NormalizeVoiceName(voiceName);
        return SongMetaUtils.GetAllNotes(songMeta.GetVoice(voiceName));
    }

    public Color GetEnumLayerColor(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].Color;
    }

    public Color GetVoiceLayerColor(string voiceName)
    {
        voiceName = Voice.NormalizeVoiceName(voiceName);
        if (voiceNameToLayerMap.TryGetValue(voiceName, out SongEditorVoiceLayer layer))
        {
            return layer.Color;
        }
        return Colors.beige;
    }

    public bool IsEnumLayerVisible(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].IsVisible;
    }

    public void SetEnumLayerVisible(ESongEditorLayer layerEnum, bool newValue)
    {
        if (newValue == layerEnumToLayerMap[layerEnum].IsVisible)
        {
            return;
        }

        layerEnumToLayerMap[layerEnum].IsVisible = newValue;
        layerChangedEventStream.OnNext(new LayerChangedEvent(layerEnum));
    }

    public bool IsVoiceLayerVisible(string voiceName)
    {
        voiceName = Voice.NormalizeVoiceName(voiceName);
        if (voiceNameToLayerMap.TryGetValue(voiceName, out SongEditorVoiceLayer layer))
        {
            return layer.IsVisible;
        }
        return true;
    }

    public void SetVoiceLayerVisible(string voiceName, bool newValue)
    {
        voiceName = Voice.NormalizeVoiceName(voiceName);
        if (newValue == voiceNameToLayerMap[voiceName].IsVisible)
        {
            return;
        }

        voiceNameToLayerMap[voiceName].IsVisible = newValue;
        layerChangedEventStream.OnNext(new LayerChangedEvent(voiceName));
    }

    public bool IsVoiceLayerEditable(string voiceName)
    {
        voiceName = Voice.NormalizeVoiceName(voiceName);
        if (voiceNameToLayerMap.TryGetValue(voiceName, out SongEditorVoiceLayer layer))
        {
            return layer.IsEditable;
        }
        return true;
    }

    public void SetVoiceLayerEditable(string voiceName, bool newValue)
    {
        voiceName = Voice.NormalizeVoiceName(voiceName);
        if (newValue == voiceNameToLayerMap[voiceName].IsEditable)
        {
            return;
        }

        voiceNameToLayerMap[voiceName].IsEditable = newValue;
        GetVoiceLayerNotes(voiceName).ForEach(note => note.IsEditable = newValue);
        layerChangedEventStream.OnNext(new LayerChangedEvent(voiceName));
    }

    public bool IsEnumLayerEditable(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].IsEditable;
    }

    public void SetEnumLayerEditable(ESongEditorLayer layerEnum, bool newValue)
    {
        if (newValue == layerEnumToLayerMap[layerEnum].IsEditable)
        {
            return;
        }

        layerEnumToLayerMap[layerEnum].IsEditable = newValue;
        GetEnumLayerNotes(layerEnum).ForEach(note => note.IsEditable = newValue);
        layerChangedEventStream.OnNext(new LayerChangedEvent(layerEnum));
    }

    public SongEditorEnumLayer GetEnumLayer(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum];
    }

    public List<SongEditorEnumLayer> GetEnumLayers()
    {
        return new List<SongEditorEnumLayer>(layerEnumToLayerMap.Values);
    }

    public List<SongEditorVoiceLayer> GetVoiceLayers()
    {
        return new List<SongEditorVoiceLayer>(voiceNameToLayerMap.Values);
    }

    private static Dictionary<string,SongEditorVoiceLayer> CreateVoiceNameToLayerMap()
    {
        Dictionary<string, SongEditorVoiceLayer> result = new();
        List<string> voiceNames = new() { Voice.firstVoiceName, Voice.secondVoiceName };
        foreach (string voiceName in voiceNames)
        {
            result.Add(voiceName, new SongEditorVoiceLayer(voiceName));
        }
        result[Voice.firstVoiceName].Color = Colors.CreateColor("#2ecc71");
        result[Voice.secondVoiceName].Color = Colors.CreateColor("#9b59b6");

        return result;
    }

    private static Dictionary<ESongEditorLayer, SongEditorEnumLayer> CreateLayerEnumToLayerMap()
    {
        Dictionary<ESongEditorLayer, SongEditorEnumLayer> result = new();
        List<ESongEditorLayer> layerEnums = EnumUtils.GetValuesAsList<ESongEditorLayer>();
        foreach (ESongEditorLayer layerEnum in layerEnums)
        {
            result.Add(layerEnum, new SongEditorEnumLayer(layerEnum));
        }
        result[ESongEditorLayer.MicRecording].Color = Colors.CreateColor("#1D67C2", 200);
        result[ESongEditorLayer.ButtonRecording].Color = Colors.CreateColor("#138BBA", 200);
        result[ESongEditorLayer.CopyPaste].Color = Colors.CreateColor("#F08080", 200);
        result[ESongEditorLayer.MidiFile].Color = Colors.CreateColor("#0F9799", 200);

        result[ESongEditorLayer.CopyPaste].IsEditable = false;

        return result;
    }

    public List<Note> GetAllEnumLayerNotes()
    {
        List<Note> notes = new();
        foreach (ESongEditorLayer layerEnum in layerEnumToLayerMap.Keys)
        {
            List<Note> notesOfLayer = GetEnumLayerNotes(layerEnum);
            notes.AddRange(notesOfLayer);
        }
        return notes;
    }

    public List<Note> GetAllVisibleEnumLayerNotes()
    {
        List<Note> notes = new();
        foreach (ESongEditorLayer layerEnum in layerEnumToLayerMap.Keys)
        {
            if (IsEnumLayerVisible(layerEnum))
            {
                List<Note> notesOfLayer = GetEnumLayerNotes(layerEnum);
                notes.AddRange(notesOfLayer);
            }
        }
        return notes;
    }

    public void RemoveNoteFromAllEnumLayers(Note note)
    {
        foreach (SongEditorEnumLayer layer in layerEnumToLayerMap.Values)
        {
            layer.RemoveNote(note);
        }
    }

    public bool IsNoteVisible(Note note)
    {
        if (note.Sentence?.Voice != null)
        {
            return IsVoiceLayerVisible(note.Sentence.Voice.Name);
        }

        if (TryGetEnumLayer(note, out SongEditorEnumLayer layer))
        {
            return IsEnumLayerVisible(layer.LayerEnum);
        }

        return true;
    }

    public bool TryGetEnumLayer(Note note, out SongEditorEnumLayer enumLayer)
    {
        foreach (SongEditorEnumLayer songEditorLayer in GetEnumLayers())
        {
            if (songEditorLayer.ContainsNote(note))
            {
                enumLayer = songEditorLayer;
                return true;
            }
        }

        enumLayer = null;
        return false;
    }

    public Color GetLayerColor(AbstractSongEditorLayer layer)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            return GetEnumLayerColor(enumLayer.LayerEnum);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            return GetVoiceLayerColor(voiceLayer.VoiceName);
        }
        return Colors.beige;
    }

    public List<Note> GetLayerNotes(AbstractSongEditorLayer layer)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            return GetEnumLayerNotes(enumLayer.LayerEnum);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            return GetVoiceLayerNotes(voiceLayer.VoiceName);
        }
        return new List<Note>();
    }

    public bool IsLayerVisible(AbstractSongEditorLayer layer)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            return IsEnumLayerVisible(enumLayer.LayerEnum);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            return IsVoiceLayerVisible(voiceLayer.VoiceName);
        }
        return true;
    }

    public void SetLayerVisible(AbstractSongEditorLayer layer, bool newValue)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            SetEnumLayerVisible(enumLayer.LayerEnum, newValue);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            SetVoiceLayerVisible(voiceLayer.VoiceName, newValue);
        }
    }

    public bool IsLayerEditable(AbstractSongEditorLayer layer)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            return IsEnumLayerEditable(enumLayer.LayerEnum);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            return IsVoiceLayerEditable(voiceLayer.VoiceName);
        }
        return true;
    }

    public void SetLayerEditable(AbstractSongEditorLayer layer, bool newValue)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            SetEnumLayerEditable(enumLayer.LayerEnum, newValue);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            SetVoiceLayerEditable(voiceLayer.VoiceName, newValue);
        }
    }
}
