using System;
using System.Collections.Generic;
using System.IO;
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
            uiManager.CreateNotification("File does not exist.", Colors.red);
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
            uiManager.CreateNotification("Loaded MIDI file successfully");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            uiManager.CreateNotification($"Loading MIDI file failed: {e.Message}", Colors.red);
        }
    }

    private List<Note> LoadNotesFromMidiFile(string midiFilePath)
    {
        List<Note> loadedNotes = new List<Note>();

        MidiFile midiFile = midiManager.LoadMidiFile(midiFilePath);
        Dictionary<int, Note> midiPitchToNoteUnderConstruction = new Dictionary<int, Note>();
        foreach (MidiTrack track in midiFile.Tracks)
        {
            foreach (MidiEvent midiEvent in track.MidiEvents)
            {
                uint deltaTimeInSamples = midiEvent.deltaTime;
                int deltaTimeInMillis = (int)(deltaTimeInSamples / (MidiManager.midiStreamSampleRateHz / 1000));

                if (midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_On)
                {
                    int midiPitch = midiEvent.parameter1;
                    Note newNote = new Note();
                    int startBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis);
                    newNote.SetStartAndEndBeat(startBeat, startBeat);
                    newNote.SetMidiNote(midiPitch);

                    if (midiPitchToNoteUnderConstruction.TryGetValue(midiPitch, out Note existingNote))
                    {
                        Debug.LogWarning($"A Note for pitch {midiPitch} already exists. It will be overwritten by a new note with this pitch.");
                    }

                    midiPitchToNoteUnderConstruction[midiPitch] = newNote;
                }

                if (midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_Off)
                {
                    int midiPitch = midiEvent.parameter1;
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
            }
        }
        Debug.Log("Loaded notes from midi file: " + loadedNotes.Count);
        return loadedNotes;
    }
}
