using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionAction : AbstractAudioClipAction
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

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
    
    private IAudioSamplesAnalyzer audioSamplesAnalyzer;
    private EPitchDetectionAlgorithm audioSamplesAnalyzerPitchDetectionAlgorithm;

    public void CreateNotesUsingBasicPitch(bool notify)
    {
        if (!FileUtils.Exists(songMeta.Mp3))
        {
            return;
        }
        string fileName = Path.GetFileName(songMeta.Mp3);
        Job pitchDetectionJob = JobManager.CreateAndAddJob($"Pitch detection of '{fileName}'");
        IObservable<BasicPitchDetectionResult> pitchDetectionObservable = pitchDetectionManager.ProcessSongMeta(songMeta, pitchDetectionJob);

        pitchDetectionObservable
            .CatchIgnore((Exception ex) =>
            {
                pitchDetectionJob.SetResult(EJobResult.Error);
                UiManager.CreateNotification("Pitch detection failed.");
            })
            .Subscribe(result =>
            {
                pitchDetectionJob.SetResult(EJobResult.Ok);
                ImportBasicPitchMidiFile(result.MidiFilePath);
                
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new NotesChangedEvent());
                }
            });
    }

    private void ImportBasicPitchMidiFile(string midiFilePath)
    {
        if (!FileUtils.Exists(midiFilePath))
        {
            Debug.LogError($"Failed to import MIDI file created by Basic Pitch. File not found: {midiFilePath}");
            UiManager.CreateNotification($"Failed to import MIDI file.");
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
            UiManager.CreateNotification("Run pitch detection first");
            return;
        }

        PitchDetectionUtils.MoveNotesToDetectedPitchUsingPitchDetectionLayer(
            songMeta,
            notes,
            pitchDetectionLayerNotes);

        if (notify)
        {
            songMetaChangeEventStream.OnNext(new NotesChangedEvent());
        }
    }
}
