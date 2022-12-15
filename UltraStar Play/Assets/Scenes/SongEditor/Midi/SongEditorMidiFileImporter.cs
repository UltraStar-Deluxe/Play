using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpSynth.Midi;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMidiFileImporter : INeedInjection
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

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public void ImportMidiFile(string midiFilePath)
    {
        if (!File.Exists(midiFilePath))
        {
            Debug.Log($"File does not exist: {midiFilePath}");
            uiManager.CreateNotificationVisualElement("File does not exist");
            return;
        }

        try
        {
            List<Note> loadedNotes = LoadNotesFromMidiFile(midiFilePath);
            // Shift notes such that the first note starts at the current playback position
            loadedNotes = ShiftNotesToPlaybackPosition(loadedNotes);

            editorNoteDisplayer.ClearNotesInLayer(ESongEditorLayer.MidiFile);
            layerManager.ClearEnumLayer(ESongEditorLayer.MidiFile);
            loadedNotes.ForEach(loadedNote => layerManager.AddNoteToEnumLayer(ESongEditorLayer.MidiFile, loadedNote));
            editorNoteDisplayer.UpdateNotes();
            uiManager.CreateNotificationVisualElement("Loaded MIDI file successfully");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            uiManager.CreateNotificationVisualElement($"Loading MIDI file failed: {e.Message}", "error");
        }
    }

    private List<Note> ShiftNotesToPlaybackPosition(List<Note> notes)
    {
        Note firstNote = notes.FindMinElement(note => note.StartBeat);
        int playbackPositionInBeats = (int)Math.Round(songAudioPlayer.GetCurrentBeat(true));
        int difference = playbackPositionInBeats - firstNote.StartBeat;
        if (difference == 0)
        {
            return notes;
        }

        return notes
            .Select(note => new Note(
                note.Type,
                note.StartBeat + difference,
                note.Length,
                note.TxtPitch,
                note.Text))
            .ToList();
    }

    private List<Note> LoadNotesFromMidiFile(string midiFilePath)
    {
        List<Note> loadedNotes = new();

        MidiFile midiFile = midiManager.LoadMidiFile(midiFilePath);
        if (midiFile == null)
        {
            throw new UnityException("Loading midi file failed.");
        }

        Dictionary<int, Note> midiPitchToNoteUnderConstruction = new();
        midiFile.Tracks.ForEach(track =>
        {
            midiPitchToNoteUnderConstruction.Clear();
            track.MidiEvents.ForEach(midiEvent =>
            {
                if (midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_On)
                {
                    HandleStartOfNote(midiEvent, midiPitchToNoteUnderConstruction);
                }

                if (midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_Off)
                {
                    HandleEndOfNote(midiEvent, midiPitchToNoteUnderConstruction, loadedNotes);
                }
            });
        });
        Debug.Log("Loaded notes from midi file: " + loadedNotes.Count);
        return loadedNotes;
    }

    private void HandleEndOfNote(MidiEvent midiEvent, Dictionary<int, Note> midiPitchToNoteUnderConstruction, List<Note> loadedNotes)
    {
        int midiPitch = midiEvent.parameter1;
        int deltaTimeInMillis = GetDeltaTimeInMillis(midiEvent);
        int endBeat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis));
        if (midiPitchToNoteUnderConstruction.TryGetValue(midiPitch, out Note existingNote))
        {
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
            Debug.LogWarning($"No Note for pitch {MidiUtils.GetAbsoluteName(midiPitch)} is being constructed. Ignoring this Note_Off event at {deltaTimeInMillis} ms.");
        }
    }

    private void HandleStartOfNote(MidiEvent midiEvent, Dictionary<int, Note> midiPitchToNoteUnderConstruction)
    {
        int midiPitch = midiEvent.parameter1;
        int deltaTimeInMillis = GetDeltaTimeInMillis(midiEvent);
        Note newNote = new();
        int startBeat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis));
        newNote.SetStartAndEndBeat(startBeat, startBeat);
        newNote.SetMidiNote(midiPitch);

        if (midiPitchToNoteUnderConstruction.ContainsKey(midiPitch))
        {
            Debug.LogWarning($"A Note with pitch {midiPitch} started but did not end before the next. The note will be ignored.");
        }

        midiPitchToNoteUnderConstruction[midiPitch] = newNote;
    }

    private static int GetDeltaTimeInMillis(MidiEvent midiEvent)
    {
        uint deltaTimeInSamples = midiEvent.deltaTime;
        int deltaTimeInMillis = (int)Math.Round(deltaTimeInSamples / (MidiManager.midiStreamSampleRateHz / 1000.0));
        return deltaTimeInMillis;
    }
}
