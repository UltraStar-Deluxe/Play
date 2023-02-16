using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpSynth.Midi;
using PrimeInputActions;
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
    private UiManager uiManager;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;
    
    public void ImportMidiFile(
        string midiFilePath,
        int trackIndex,
        int channelIndex,
        bool importWithLyrics,
        string voiceName)
    {
        if (!File.Exists(midiFilePath))
        {
            Debug.Log($"File does not exist: {midiFilePath}");
            UiManager.CreateNotification("File does not exist");
            return;
        }

        MidiFile midiFile = midiManager.LoadMidiFile(midiFilePath);
        if (midiFile == null)
        {
            throw new UnityException("Loading midi file failed.");
        }
        
        try
        {
            List<Note> loadedNotes = LoadNotesFromMidiFile(midiFile, trackIndex, channelIndex, importWithLyrics);
            
            if (voiceName == null)
            {
                // Add all notes to dedicated MIDI layer
                layerManager.ClearEnumLayer(ESongEditorLayer.MidiFile);
                loadedNotes.ForEach(loadedNote => layerManager.AddNoteToEnumLayer(ESongEditorLayer.MidiFile, loadedNote));
            }
            else
            {
                // Assign notes to player
                MidiTrack track = midiFile.Tracks[trackIndex];
                MoveNotesToVoice(loadedNotes, voiceName, track);
            }
            
            // Shift notes such that the first note starts at the current playback position
            ShiftNotesToPlaybackPosition(loadedNotes);

            songMetaChangeEventStream.OnNext(new ImportedMidiFileEvent());
            
            UiManager.CreateNotification("Loaded MIDI file successfully");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            UiManager.CreateNotification($"Loading MIDI file failed: {e.Message}");
        }
    }

    private void MoveNotesToVoice(List<Note> loadedNotes, string voiceName, MidiTrack track)
    {
        // Search for line breaks in lyrics of the channel.
        // A line break starts a new sentence.
        List<Note> notesWithoutGroup = loadedNotes.ToList();
        List<List<Note>> noteGroups = new();
        GroupNotesByLineBreak();
        
        void GroupNotesByLineBreak()
        {
            List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(track);
            if (lyricsEvents.IsNullOrEmpty())
            {
                return;
            }

            lyricsEvents.ForEach(midiEvent =>
            {
                string midiEventLyrics = MidiFileUtils.GetLyrics(midiEvent);
                if (!midiEventLyrics.EndsWith("\n"))
                {
                    // This is not a line break.
                    return;
                }

                int deltaTimeInMillis = MidiFileUtils.GetDeltaTimeInMillis(midiEvent);
                int beat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis));
                List<Note> correspondingNotes = notesWithoutGroup
                    .Where(note => note.StartBeat <= beat)
                    .ToList();
                if (!correspondingNotes.IsNullOrEmpty())
                {
                    noteGroups.Add(correspondingNotes);
                    notesWithoutGroup.RemoveAll(correspondingNotes);
                }
            });
        }

        if (noteGroups.IsNullOrEmpty())
        {
            // No line breaks found in lyrics such that there are no groups.
            // Thus, split into groups here.
            noteGroups = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, loadedNotes);
        }
        
        noteGroups.ForEach(notesGroup =>
        {
            MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, notesGroup, voiceName);
        });
    }

    public MidiFile LoadMidiFile(string filePath)
    {
        return midiManager.LoadMidiFile(filePath);
    }
    
    private void ShiftNotesToPlaybackPosition(List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return;
        }

        Note firstNote = notes.FindMinElement(note => note.StartBeat);
        int playbackPositionInBeats = (int)Math.Round(songAudioPlayer.GetCurrentBeat(true));
        int difference = playbackPositionInBeats - firstNote.StartBeat;
        if (difference == 0)
        {
            return;
        }

        notes.ForEach(note =>
        {
            note.SetStartAndEndBeat(note.StartBeat + difference, note.EndBeat + difference);
        });
    }

    public List<Note> LoadNotesFromMidiFile(
        MidiFile midiFile,
        int trackIndex,
        int channelIndex,
        bool importWithLyrics)
    {
        List<Note> loadedNotes = new();
        Dictionary<int, Note> midiPitchToNoteUnderConstruction = new();
        
        void LoadNotesFromTrack(MidiTrack track)
        {
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

            List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(track);
            if (lyricsEvents.IsNullOrEmpty())
            {
                return;
            }
            
            lyricsEvents.ForEach(midiEvent =>
            {
                string midiEventLyrics = MidiFileUtils.GetLyrics(midiEvent);
                if (midiEventLyrics.IsNullOrEmpty())
                {
                    return;
                }

                int deltaTimeInMillis = MidiFileUtils.GetDeltaTimeInMillis(midiEvent);
                int beat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis));
                Note correspondingNote = notesWithoutText.FirstOrDefault(note => SongMetaUtils.IsBeatInNote(note, beat));
                if (correspondingNote != null)
                {
                    notesWithoutText.Remove(correspondingNote);
                    correspondingNote.SetText(midiEventLyrics);
                }
                else
                {
                    // Find best matching note within a tolerance.
                    Note bestMatch = notesWithoutText.FindMinElement(note => Math.Abs(note.StartBeat - beat));
                    double distanceInMillis = Math.Abs(bestMatch.StartBeat - beat) * BpmUtils.MillisecondsPerBeat(songMeta);
                    if (distanceInMillis < 1000)
                    {
                        notesWithoutText.Remove(bestMatch);
                        bestMatch.SetText(midiEventLyrics);
                    }
                }
            });

            // Normalize spaces.
            Note lastNote = null;
            foreach (Note note in loadedNotes)
            {
                if (note.Text.Contains("\n"))
                {
                    note.SetText(note.Text.Replace("\n", ""));
                }
                
                if (lastNote != null
                    && note.Text.StartsWith(" ")
                    && !lastNote.Text.EndsWith(" "))
                {
                    lastNote.SetText(lastNote.Text + " ");
                    note.SetText(note.Text.Substring(1));
                }
                
                lastNote = note;
            }
        }
        
        if (trackIndex < midiFile.Tracks.Length)
        {
            MidiTrack track = midiFile.Tracks[trackIndex];
            LoadNotesFromTrack(track);
            if (loadedNotes.IsNullOrEmpty())
            {
                throw new UltraStarPlayException($"No notes found in channel {channelIndex} of track {trackIndex}");
            }

            if (importWithLyrics)
            {
                LoadLyricsFromTrack(track);
            }
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
        int deltaTimeInMillis = MidiFileUtils.GetDeltaTimeInMillis(midiEvent);
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
        int deltaTimeInMillis = MidiFileUtils.GetDeltaTimeInMillis(midiEvent);
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
}
