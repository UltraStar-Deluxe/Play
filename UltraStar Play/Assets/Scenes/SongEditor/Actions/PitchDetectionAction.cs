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
        string fileName = Path.GetFileName(songMeta.Audio);
        Job pitchDetectionJob = JobManager.CreateAndAddJob(Translation.Get(R.Messages.job_pitchDetectionWithName,
            "name", fileName));
        IObservable<BasicPitchDetectionResult> pitchDetectionObservable = pitchDetectionManager.ProcessSongMetaAsObservable(songMeta, pitchDetectionJob);

        pitchDetectionObservable
            .CatchIgnore((Exception ex) =>
            {
                pitchDetectionJob.SetResult(EJobResult.Error);
                NotificationManager.CreateNotification(Translation.Get(R.Messages.job_pitchDetection_errorWithReason,
                    "reason", ex.Message));
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
