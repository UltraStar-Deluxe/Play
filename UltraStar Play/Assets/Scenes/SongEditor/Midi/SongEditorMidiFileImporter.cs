using System;
using System.Collections.Generic;
using System.IO;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;
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
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private Settings settings;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    public void ImportMidiFile(
        string midiFilePath,
        int trackIndex,
        int channelIndex,
        bool importLyrics,
        bool importNotes,
        EVoiceId? voiceId,
        bool shiftNotesToPlaybackPosition,
        ESongEditorLayer layer)
    {
        if (!importLyrics
            && !importNotes)
        {
            return;
        }

        if (!File.Exists(midiFilePath))
        {
            Debug.Log($"File does not exist: {midiFilePath}");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_fileNotFound));
            return;
        }

        // Remove old notes
        editorNoteDisplayer.ClearNotesInLayer(layer);
        layerManager.ClearEnumLayer(layer);

        MidiFile midiFile = MidiFileUtils.LoadMidiFile(midiFilePath);
        if (midiFile == null)
        {
            throw new UnityException("Loading midi file failed.");
        }

        try
        {
            MidiFileUtils.CalculateMidiEventTimesInMillis(
                midiFile,
                out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
                out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

            List<Note> loadedNotes = MidiToSongMetaUtils.LoadNotesFromMidiFile(songMeta, midiFile, trackIndex, channelIndex, importLyrics, importNotes, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);

            if (voiceId == null)
            {
                // Add all notes to dedicated layer
                layerManager.ClearEnumLayer(layer);
                loadedNotes.ForEach(loadedNote =>
                {
                    layerManager.AddNoteToEnumLayer(layer, loadedNote);
                    loadedNote.IsEditable = layerManager.IsLayerEditable(layerManager.GetEnumLayer(layer));
                });
            }
            else if (voiceId is { } nonNullVoiceId)
            {
                // Assign notes to player
                MidiTrack track = midiFile.Tracks[trackIndex];
                MidiToSongMetaUtils.AssignNotesToVoice(songMeta, loadedNotes, nonNullVoiceId, track, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            }

            if (shiftNotesToPlaybackPosition)
            {
                // Shift notes such that the first note starts at the current playback position
                ShiftNotesToPlaybackPosition(loadedNotes);
            }

            songMetaChangedEventStream.OnNext(new ImportedMidiFileEvent());
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
        }
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
}
