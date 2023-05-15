using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;
    private EPitchDetectionAlgorithm audioSamplesAnalyzerPitchDetectionAlgorithm;

    public void MoveNotesToDetectedPitch(List<Note> notes, bool notify, ESongEditorSamplesSource samplesSource)
    {
        if (notes.IsNullOrEmpty())
        {
            return;
        }
        
        AudioClip audioClip = GetAudioClip(samplesSource);
        if (audioClip == null)
        {
            return;
        }

        PitchDetectionUtils.MoveNotesToDetectedPitch(
                songMeta,
                notes,
                audioClip,
                settings.SongEditorSettings.PitchDetectionAlgorithm)
            .Subscribe(_ =>
            {
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new NotesChangedEvent());
                }
            });
    }

    public void CreateNotesForDetectedPitch(int startBeat, int lengthInBeats, ESongEditorSamplesSource samplesSource, bool notify)
    {
        AudioClip audioClip = GetAudioClip(samplesSource);
        if (audioClip == null)
        {
            return;
        }

        int endBeat = startBeat + lengthInBeats;
        
        // Remove old analyzed notes
        songEditorLayerManager.GetEnumLayerNotes(ESongEditorLayer.PitchDetection)
            .Where(oldNote =>
                oldNote.StartBeat >= startBeat && oldNote.EndBeat <= startBeat + lengthInBeats)
            .ForEach(oldNote =>
            {
                editorNoteDisplayer.RemoveNoteControl(oldNote);
                songEditorLayerManager.RemoveNoteFromAllEnumLayers(oldNote);
            });

        Job pitchDetectionJob = new("Pitch detection");
        jobManager.AddJob(pitchDetectionJob);
        pitchDetectionJob.SetStatus(EJobStatus.Running);
        pitchDetectionJob.EstimatedTotalDurationInMillis = PitchDetectionUtils.GetEstimatedPitchDetectionDurationInMillis(songMeta, lengthInBeats);

        PitchDetectionUtils.DoPitchDetectionAsObservable(
                songMeta,
                audioClip,
                startBeat,
                lengthInBeats,
                settings.SongEditorSettings.PitchDetectionAlgorithm)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                pitchDetectionJob.SetResult(EJobResult.Error);
            })
            .Subscribe(pitchDetectionResult =>
            {
                pitchDetectionJob.SetResult(EJobResult.Ok);

                List<Note> createdNotes = PitchDetectionUtils.CreateNotesForPitchDetectionResult(pitchDetectionResult);

                // Add created notes to song editor layer
                createdNotes.ForEach(createdNote =>
                {
                    if (createdNote.EndBeat > endBeat)
                    {
                        createdNote.SetEndBeat(endBeat);
                    }
                    
                    // IsEditable must be set AFTER the notes have been set completely. Otherwise SetLength will not work.
                    createdNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.PitchDetection);
                    songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.PitchDetection, createdNote);
                });

                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new NotesChangedEvent());
                }
            });
    }
}
