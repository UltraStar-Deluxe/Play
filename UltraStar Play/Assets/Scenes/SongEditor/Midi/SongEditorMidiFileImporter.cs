using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpSynth.Midi;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMidiFileImporter : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private Settings settings;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private UiManager uiManager;

    public void ImportMidiFile()
    {
        string midiFilePath = settings.SongEditorSettings.MidiFilePath;
        if (!File.Exists(midiFilePath))
        {
            uiManager.CreateNotificationVisualElement("File does not exist.", "error");
            return;
        }

        try
        {
            List<Note> loadedNotes = LoadNotesFromMidiFile(midiFilePath);
            layerManager.ClearLayer(ESongEditorLayer.MidiFile);
            foreach (Note loadedNote in loadedNotes)
            {
                layerManager.AddNoteToLayer(ESongEditorLayer.MidiFile, loadedNote);
            }
            editorNoteDisplayer.UpdateNotes();
            uiManager.CreateNotificationVisualElement("Loaded MIDI file successfully");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            uiManager.CreateNotificationVisualElement($"Loading MIDI file failed: {e.Message}", "error");
        }
    }

    private List<Note> LoadNotesFromMidiFile(string midiFilePath)
    {
        List<Note> loadedNotes = new List<Note>();

        MidiFile midiFile = midiManager.LoadMidiFile(midiFilePath);
        if (midiFile == null)
        {
            throw new UnityException("Loading midi file failed.");
        }

        Dictionary<int, Note> midiPitchToNoteUnderConstruction = new Dictionary<int, Note>();
        foreach (MidiEvent midiEvent in midiFile.Tracks.SelectMany(track => track.MidiEvents))
        {
            if (midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_On)
            {
                HandleStartOfNote(midiEvent, midiPitchToNoteUnderConstruction);
            }

            if (midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_Off)
            {
                HandleEndOfNote(midiEvent, midiPitchToNoteUnderConstruction, loadedNotes);
            }
        }
        Debug.Log("Loaded notes from midi file: " + loadedNotes.Count);
        return loadedNotes;
    }

    private void HandleEndOfNote(MidiEvent midiEvent, Dictionary<int, Note> midiPitchToNoteUnderConstruction, List<Note> loadedNotes)
    {
        int midiPitch = midiEvent.parameter1;
        int deltaTimeInMillis = GetDeltaTimeInMillis(midiEvent);
        if (midiPitchToNoteUnderConstruction.TryGetValue(midiPitch, out Note existingNote))
        {
            int endBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis);
            if (endBeat > existingNote.StartBeat)
            {
                existingNote.SetEndBeat(endBeat);
                loadedNotes.Add(existingNote);
            }
            else
            {
                Debug.LogWarning($"End beat {endBeat} is not after start beat {existingNote.StartBeat}. Skipping this note.");
            }
            midiPitchToNoteUnderConstruction.Remove(midiPitch);
        }
        else
        {
            Debug.LogWarning($"No Note for pitch {midiPitch} is beeing constructed. Ignoring this Note_Off event.");
        }
    }

    private void HandleStartOfNote(MidiEvent midiEvent, Dictionary<int, Note> midiPitchToNoteUnderConstruction)
    {
        int midiPitch = midiEvent.parameter1;
        int deltaTimeInMillis = GetDeltaTimeInMillis(midiEvent);
        Note newNote = new Note();
        int startBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis);
        newNote.SetStartAndEndBeat(startBeat, startBeat);
        newNote.SetMidiNote(midiPitch);

        if (midiPitchToNoteUnderConstruction.ContainsKey(midiPitch))
        {
            Debug.LogWarning($"A Note with pitch {midiPitch} started but did not end before the next. The note will be ignored.");
        }

        midiPitchToNoteUnderConstruction[midiPitch] = newNote;
    }

    private int GetDeltaTimeInMillis(MidiEvent midiEvent)
    {
        uint deltaTimeInSamples = midiEvent.deltaTime;
        int deltaTimeInMillis = (int)(deltaTimeInSamples / (MidiManager.midiStreamSampleRateHz / 1000));
        return deltaTimeInMillis;
    }
}
