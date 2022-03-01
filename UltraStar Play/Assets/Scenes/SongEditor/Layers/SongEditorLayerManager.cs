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

public class SongEditorLayerManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    private readonly Dictionary<ESongEditorLayer, SongEditorLayer> layerEnumToLayerMap = CreateLayerEnumToLayerMap();

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private Settings settings;

    private readonly Subject<LayerChangedEvent> layerChangedEventStream = new Subject<LayerChangedEvent>();
    public IObservable<LayerChangedEvent> LayerChangedEventStream => layerChangedEventStream;

    public void OnSceneInjectionFinished()
    {
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (changeEvent is MovedNotesToVoiceEvent mntve)
        {
            mntve.notes
                .Where(note => note.Sentence != null)
                .ForEach(note => RemoveNoteFromAllLayers(note));
        }
    }
    
    public void AddNoteToLayer(ESongEditorLayer layerEnum, Note note)
    {
        layerEnumToLayerMap[layerEnum].AddNote(note);
    }

    public void ClearLayer(ESongEditorLayer layerEnum)
    {
        layerEnumToLayerMap[layerEnum].ClearNotes();
    }

    public List<Note> GetNotes(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].GetNotes();
    }

    public Color GetColor(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].Color;
    }

    public bool IsLayerEnabled(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].IsEnabled;
    }

    public void SetLayerEnabled(ESongEditorLayer layerEnum, bool newValue)
    {
        layerEnumToLayerMap[layerEnum].IsEnabled = newValue;
        layerChangedEventStream.OnNext(new LayerChangedEvent(layerEnum));
    }

    public List<SongEditorLayer> GetLayers()
    {
        return new List<SongEditorLayer>(layerEnumToLayerMap.Values);
    }

    private static Dictionary<ESongEditorLayer, SongEditorLayer> CreateLayerEnumToLayerMap()
    {
        Dictionary<ESongEditorLayer, SongEditorLayer> result = new Dictionary<ESongEditorLayer, SongEditorLayer>();
        List<ESongEditorLayer> layerEnums = EnumUtils.GetValuesAsList<ESongEditorLayer>();
        foreach (ESongEditorLayer layerEnum in layerEnums)
        {
            result.Add(layerEnum, new SongEditorLayer(layerEnum));
        }
        result[ESongEditorLayer.MicRecording].Color = Colors.CreateColor("#1D67C2");
        result[ESongEditorLayer.ButtonRecording].Color = Colors.CreateColor("#138BBA");
        result[ESongEditorLayer.CopyPaste].Color = Colors.CreateColor("#F08080", 200);
        result[ESongEditorLayer.MidiFile].Color = Colors.CreateColor("#0F9799");
        return result;
    }

    public List<Note> GetAllNotes()
    {
        List<Note> notes = new List<Note>();
        foreach (ESongEditorLayer layerEnum in layerEnumToLayerMap.Keys)
        {
            List<Note> notesOfLayer = GetNotes(layerEnum);
            notes.AddRange(notesOfLayer);
        }
        return notes;
    }

    public List<Note> GetAllVisibleNotes()
    {
        List<Note> notes = new List<Note>();
        foreach (ESongEditorLayer layerEnum in layerEnumToLayerMap.Keys)
        {
            if (IsLayerEnabled(layerEnum))
            {
                List<Note> notesOfLayer = GetNotes(layerEnum);
                notes.AddRange(notesOfLayer);
            }
        }
        return notes;
    }

    public void RemoveNoteFromAllLayers(Note note)
    {
        foreach (SongEditorLayer layer in layerEnumToLayerMap.Values)
        {
            layer.RemoveNote(note);
        }
    }

    public bool IsVisible(Note note)
    {
        if (note.Sentence?.Voice != null)
        {
            return IsVoiceVisible(note.Sentence.Voice);
        }

        if (TryGetLayer(note, out SongEditorLayer layer))
        {
            return IsLayerEnabled(layer.LayerEnum);
        }

        return true;
    }

    public bool TryGetLayer(Note note, out SongEditorLayer layer)
    {
        foreach (SongEditorLayer songEditorLayer in GetLayers())
        {
            if (songEditorLayer.ContainsNote(note))
            {
                layer = songEditorLayer;
                return true;
            }
        }

        layer = null;
        return false;
    }

    public bool IsVoiceVisible(Voice voice)
    {
        return !IsVoiceHidden(voice);
    }

    public bool IsVoiceHidden(Voice voice)
    {
        return voice != null
               && settings.SongEditorSettings.HideVoices.AnyMatch(voiceName => voice.VoiceNameEquals(voiceName));
    }
}
