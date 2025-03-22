using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionAction : AbstractAudioClipAction
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private PitchDetectionManager pitchDetectionManager;

    [Inject]
    private SongEditorMidiFileImporter songEditorMidiFileImporter;

    public async void CreateNotesUsingBasicPitch(bool notify)
    {
        PitchDetectionResult pitchDetectionResult = await pitchDetectionManager.ProcessSongMetaJob(songMeta).GetResultAsync();
        ImportBasicPitchMidiFile(pitchDetectionResult.MidiFilePath);

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }
    }

    private void ImportBasicPitchMidiFile(string midiFilePath)
    {
        if (!FileUtils.Exists(midiFilePath))
        {
            Debug.LogError($"Failed to import MIDI file created by Basic Pitch. File not found: {midiFilePath}");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_fileNotFoundWithName,
                "name", midiFilePath));
            return;
        }
        songEditorMidiFileImporter.ImportMidiFile(
            midiFilePath,
            1,
            0,
            false,
            true,
            null,
            false,
            ESongEditorLayer.PitchDetection);
    }

    public void MoveNotesToDetectedPitchUsingPitchDetectionLayer(List<Note> notes, bool notify)
    {
        List<Note> pitchDetectionLayerNotes = songEditorLayerManager.GetLayerNotes(songEditorLayerManager.GetEnumLayer(ESongEditorLayer.PitchDetection));
        if (pitchDetectionLayerNotes.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_error_missingPitchDetection));
            return;
        }

        PitchDetectionNoteMover.MoveNotesToDetectedPitchUsingPitchDetectionLayer(
            songMeta,
            notes,
            pitchDetectionLayerNotes);

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }
    }
}
