using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorLayerManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    private Dictionary<ESongEditorLayer, SongEditorEnumLayer> layerEnumToLayerMap;
    private Dictionary<EVoiceId, SongEditorVoiceLayer> voiceIdToLayerMap;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private ThemeManager themeManager;

    private readonly Subject<LayerChangedEvent> layerChangedEventStream = new();
    public IObservable<LayerChangedEvent> LayerChangedEventStream => layerChangedEventStream;

    private Dictionary<string, Color32> songEditorLayerNameToColor;

    public void OnSceneInjectionFinished()
    {
        songEditorLayerNameToColor = themeManager.GetSongEditorLayerColors();
        layerEnumToLayerMap = CreateLayerEnumToLayerMap();
        voiceIdToLayerMap = CreateVoiceIdToLayerMap();songMetaChangedEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(SongMetaChangedEvent changedEvent)
    {
        if (changedEvent is MovedNotesToVoiceEvent mntve)
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

    public List<Note> GetVoiceLayerNotes(EVoiceId voiceId)
    {
        return SongMetaUtils.GetAllNotes(SongMetaUtils.GetVoiceById(songMeta, voiceId));
    }

    public Color GetEnumLayerColor(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].Color;
    }

    public Color GetVoiceLayerColor(EVoiceId voiceId)
    {
        if (voiceIdToLayerMap.TryGetValue(voiceId, out SongEditorVoiceLayer layer))
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

    public bool IsVoiceLayerVisible(Voice voice)
    {
        if (voice == null)
        {
            return false;
        }

        return IsVoiceLayerVisible(voice.Id);
    }

    public bool IsVoiceLayerVisible(EVoiceId voiceId)
    {
        if (voiceIdToLayerMap.TryGetValue(voiceId, out SongEditorVoiceLayer layer))
        {
            return layer.IsVisible;
        }
        return true;
    }

    public void SetVoiceLayerVisible(EVoiceId voiceId, bool newValue)
    {
        if (newValue == voiceIdToLayerMap[voiceId].IsVisible)
        {
            return;
        }

        voiceIdToLayerMap[voiceId].IsVisible = newValue;
        layerChangedEventStream.OnNext(new LayerChangedEvent(voiceId));
    }

    public bool IsVoiceLayerEditable(EVoiceId voiceId)
    {
        if (voiceIdToLayerMap.TryGetValue(voiceId, out SongEditorVoiceLayer layer))
        {
            return layer.IsEditable;
        }
        return true;
    }

    public void SetVoiceLayerEditable(EVoiceId voiceId, bool newValue)
    {
        if (newValue == voiceIdToLayerMap[voiceId].IsEditable)
        {
            return;
        }

        voiceIdToLayerMap[voiceId].IsEditable = newValue;
        GetVoiceLayerNotes(voiceId).ForEach(note => note.IsEditable = newValue);
        layerChangedEventStream.OnNext(new LayerChangedEvent(voiceId));
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
        return new List<SongEditorVoiceLayer>(voiceIdToLayerMap.Values);
    }

    private Dictionary<EVoiceId, SongEditorVoiceLayer> CreateVoiceIdToLayerMap()
    {
        Dictionary<EVoiceId, SongEditorVoiceLayer> result = new();
        List<EVoiceId> voiceIds = new() { EVoiceId.P1, EVoiceId.P2 };
        foreach (EVoiceId voiceId in voiceIds)
        {
            result.Add(voiceId, new SongEditorVoiceLayer(voiceId));
        }
        result[EVoiceId.P1].Color =  GetSongEditorLayerColor(EVoiceId.P1);
        result[EVoiceId.P2].Color =  GetSongEditorLayerColor(EVoiceId.P2);

        return result;
    }

    private Dictionary<ESongEditorLayer, SongEditorEnumLayer> CreateLayerEnumToLayerMap()
    {
        Dictionary<ESongEditorLayer, SongEditorEnumLayer> result = new();
        List<ESongEditorLayer> layerEnums = EnumUtils.GetValuesAsList<ESongEditorLayer>();
        foreach (ESongEditorLayer layerEnum in layerEnums)
        {
            result.Add(layerEnum, new SongEditorEnumLayer(layerEnum));
        }

        result[ESongEditorLayer.PitchDetection].IsEditable = false;
        result[ESongEditorLayer.PitchDetection].IsMidiSoundPlayAlongEnabled = false;
        result[ESongEditorLayer.SpeechRecognition].IsMidiSoundPlayAlongEnabled = false;
        result[ESongEditorLayer.ButtonRecording].IsMidiSoundPlayAlongEnabled = false;

        result.ForEach(entry => entry.Value.Color = GetSongEditorLayerColor(entry.Key));

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
            return IsVoiceLayerVisible(note.Sentence.Voice);
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
            return GetVoiceLayerColor(voiceLayer.VoiceId);
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
            return GetVoiceLayerNotes(voiceLayer.VoiceId);
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
            return IsVoiceLayerVisible(voiceLayer.VoiceId);
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
            SetVoiceLayerVisible(voiceLayer.VoiceId, newValue);
        }
    }

    public bool TryGetLayerEnumOfNote(Note note, out ESongEditorLayer layerEnum)
    {
        if (note.Sentence != null
            && note.Sentence.Voice != null
            && voiceIdToLayerMap.TryGetValue(note.Sentence.Voice.Id, out SongEditorVoiceLayer voiceLayer))
        {
            layerEnum = ESongEditorLayer.ButtonRecording;
            return false;
        }

        foreach (KeyValuePair<ESongEditorLayer, SongEditorEnumLayer> entry in layerEnumToLayerMap)
        {
            SongEditorEnumLayer enumLayer = entry.Value;
            if (enumLayer.ContainsNote(note))
            {
                layerEnum = entry.Key;
                return true;
            }
        }

        layerEnum = ESongEditorLayer.ButtonRecording;
        return false;
    }

    public AbstractSongEditorLayer GetLayer(Note note)
    {
        if (note.Sentence != null
            && note.Sentence.Voice != null
            && voiceIdToLayerMap.TryGetValue(note.Sentence.Voice.Id, out SongEditorVoiceLayer voiceLayer))
        {
            return voiceLayer;
        }

        foreach (KeyValuePair<ESongEditorLayer, SongEditorEnumLayer> entry in layerEnumToLayerMap)
        {
            SongEditorEnumLayer enumLayer = entry.Value;
            if (enumLayer.ContainsNote(note))
            {
                return enumLayer;
            }
        }

        return null;
    }

    public bool IsMidiSoundPlayAlongEnabled(AbstractSongEditorLayer layer)
    {
        if (layer == null)
        {
            return false;
        }

        if (layer is SongEditorEnumLayer enumLayer)
        {
            return IsEnumLayerMidiSoundPlayAlongEnabled(enumLayer.LayerEnum);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            return IsVoiceLayerMidiSoundPlayAlongEnabled(voiceLayer.VoiceId);
        }
        return true;
    }

    private bool IsVoiceLayerMidiSoundPlayAlongEnabled(EVoiceId voiceId)
    {
        if (voiceIdToLayerMap.TryGetValue(voiceId, out SongEditorVoiceLayer layer))
        {
            return layer.IsMidiSoundPlayAlongEnabled;
        }
        return true;
    }

    private bool IsEnumLayerMidiSoundPlayAlongEnabled(ESongEditorLayer layerEnum)
    {
        return layerEnumToLayerMap[layerEnum].IsMidiSoundPlayAlongEnabled;
    }

    public void SetMidiSoundPlayAlongEnabled(AbstractSongEditorLayer layer, bool newValue)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            SetEnumLayerMidiSoundPlayAlongEnabled(enumLayer.LayerEnum, newValue);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            SetVoiceLayerMidiSoundPlayAlongEnabled(voiceLayer.VoiceId, newValue);
        }
    }

    private void SetVoiceLayerMidiSoundPlayAlongEnabled(EVoiceId voiceId, bool newValue)
    {
        if (newValue == voiceIdToLayerMap[voiceId].IsMidiSoundPlayAlongEnabled)
        {
            return;
        }

        voiceIdToLayerMap[voiceId].IsMidiSoundPlayAlongEnabled = newValue;
    }

    private void SetEnumLayerMidiSoundPlayAlongEnabled(ESongEditorLayer layerEnum, bool newValue)
    {
        layerEnumToLayerMap[layerEnum].IsMidiSoundPlayAlongEnabled = newValue;
    }

    public bool IsLayerEditable(AbstractSongEditorLayer layer)
    {
        if (layer is SongEditorEnumLayer enumLayer)
        {
            return IsEnumLayerEditable(enumLayer.LayerEnum);
        }
        else if (layer is SongEditorVoiceLayer voiceLayer)
        {
            return IsVoiceLayerEditable(voiceLayer.VoiceId);
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
            SetVoiceLayerEditable(voiceLayer.VoiceId, newValue);
        }
    }

    private Color32 GetSongEditorLayerColor(ESongEditorLayer songEditorLayerEnum)
    {
        return songEditorLayerNameToColor
            .FirstOrDefault(entry => string.Equals(entry.Key, songEditorLayerEnum.ToString(), StringComparison.InvariantCultureIgnoreCase))
            .Value;
    }

    private Color32 GetSongEditorLayerColor(EVoiceId voiceId)
    {
        return songEditorLayerNameToColor
            .FirstOrDefault(entry => string.Equals(entry.Key, voiceId.ToString(), StringComparison.InvariantCultureIgnoreCase))
            .Value;
    }
}
