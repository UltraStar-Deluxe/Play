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

    public void ImportMidiFile(string midiFilePath, int trackIndex, int channelIndex)
    {
        if (!File.Exists(midiFilePath))
        {
            Debug.Log($"File does not exist: {midiFilePath}");
            UiManager.CreateNotification("File does not exist");
            return;
        }

        try
        {
            List<Note> loadedNotes = LoadNotesFromMidiFile(midiFilePath, trackIndex, channelIndex);
            
            // Shift notes such that the first note starts at the current playback position
            loadedNotes = ShiftNotesToPlaybackPosition(loadedNotes);

            editorNoteDisplayer.ClearNotesInLayer(ESongEditorLayer.MidiFile);
            layerManager.ClearEnumLayer(ESongEditorLayer.MidiFile);
            loadedNotes.ForEach(loadedNote => layerManager.AddNoteToEnumLayer(ESongEditorLayer.MidiFile, loadedNote));
            editorNoteDisplayer.UpdateNotes();
            UiManager.CreateNotification("Loaded MIDI file successfully");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            UiManager.CreateNotification($"Loading MIDI file failed: {e.Message}");
        }
    }

    private List<Note> ShiftNotesToPlaybackPosition(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return new();
        }

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

    private List<Note> LoadNotesFromMidiFile(string midiFilePath, int trackIndex, int channelIndex)
    {
        List<Note> loadedNotes = new();

        MidiFile midiFile = midiManager.LoadMidiFile(midiFilePath);
        if (midiFile == null)
        {
            throw new UnityException("Loading midi file failed.");
        }

        void LoadNotesFromTrack(MidiTrack track)
        {
            Dictionary<int, Note> midiPitchToNoteUnderConstruction = new();
            midiPitchToNoteUnderConstruction.Clear();
            
            List<MidiEvent> midiEventsOfChannel = track.MidiEvents
                .Where(midiEvent => midiEvent.channel == channelIndex)
                .ToList();
            if (midiEventsOfChannel.IsNullOrEmpty())
            {
                throw new UltraStarPlayException($"No midi event in channel {channelIndex}");
            }
            
            midiEventsOfChannel.ForEach(midiEvent =>
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
        }

        void LoadLyricsFromTrack(MidiTrack track)
        {
            List<Note> notesWithoutText = loadedNotes
                .Where(note => note.Text.IsNullOrEmpty())
                .ToList();
            
            List<MidiEvent> lyricsEvents = track.MidiEvents
                .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Lyric_Text)
                .ToList();
            List<MidiEvent> textEvent = track.MidiEvents
                .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Text_Event)
                .ToList();
            List<MidiEvent> markerTextEvents = track.MidiEvents
                .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Marker_Text)
                .ToList();
            List<MidiEvent> actualLyricsEvents = new List<List<MidiEvent>> { lyricsEvents, textEvent, markerTextEvents }
                .FindMaxElement(events => events.Count);

            if (actualLyricsEvents.IsNullOrEmpty())
            {
                return;
            }
            
            actualLyricsEvents.ForEach(midiEvent =>
            {
                if (midiEvent.Parameters.IsNullOrEmpty() || midiEvent.Parameters[0] is not string)
                {
                    return;
                }

                string midiEventText = midiEvent.Parameters[0] as string;
                int deltaTimeInMillis = GetDeltaTimeInMillis(midiEvent);
                int beat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis));
                Note correspondingNote = notesWithoutText.FirstOrDefault(note => SongMetaUtils.IsBeatInNote(note, beat));
                if (correspondingNote != null)
                {
                    notesWithoutText.Remove(correspondingNote);
                    correspondingNote.SetText(midiEventText);
                }
            });
        }
        
        if (trackIndex < midiFile.Tracks.Length)
        {
            MidiTrack track = midiFile.Tracks[trackIndex];
            LoadNotesFromTrack(track);
            if (loadedNotes.IsNullOrEmpty())
            {
                throw new UltraStarPlayException($"No notes found in channel {channelIndex} of track {trackIndex}");
            }
            
            LoadLyricsFromTrack(track);
        }
        else
        {
            throw new UltraStarPlayException($"No track with index {trackIndex}");
        }

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
