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

        int minBeat = SongMetaUtils.MinBeat(notes);
        int maxBeat = SongMetaUtils.MaxBeat(notes);
        List<Note> pitchDetectionLayerNotesInRange = pitchDetectionLayerNotes
            .Where(it => minBeat <= it.EndBeat && it.StartBeat <= maxBeat)
            .ToList();
        if (pitchDetectionLayerNotesInRange.IsNullOrEmpty())
        {
            return;
        }

        // Map beat to detected pitches
        Dictionary<int, List<int>> beatToDetectedPitches = new();
        foreach (Note pitchDetectionLayerNote in pitchDetectionLayerNotesInRange)
        {
            for (int beat = pitchDetectionLayerNote.StartBeat; beat < pitchDetectionLayerNote.EndBeat; beat++)
            {
                beatToDetectedPitches.AddInsideList(beat, pitchDetectionLayerNote.MidiNote);
            }
        }
        
        int localAverageWindowSizeInBeats = (int)BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, 3000);
        localAverageWindowSizeInBeats = NumberUtils.Limit(localAverageWindowSizeInBeats, 1, int.MaxValue);
        
        foreach (Note note in notes)
        {
            // Move note to pitch that is closest to local average on pitch detection layer
            List<int> detectedPitchesOfNote = new();
            for (int beat = note.StartBeat; beat < note.EndBeat; beat++)
            {
                if (beatToDetectedPitches.ContainsKey(beat))
                {
                    List<int> detectedPitchesOfBeat = beatToDetectedPitches[beat];
                    if (detectedPitchesOfBeat.Count == 1)
                    {
                        detectedPitchesOfNote.Add(detectedPitchesOfBeat[0]);
                    }
                    else if (detectedPitchesOfBeat.Count > 1)
                    {
                        if (TryFindLocalAveragePitch(pitchDetectionLayerNotes, beat, localAverageWindowSizeInBeats, out int localAveragePitch))
                        {
                            int detectedPitchOfBeatClosestToAverage = detectedPitchesOfBeat.FindMinElement(pitch => Math.Abs(pitch - localAveragePitch));
                            detectedPitchesOfNote.Add(detectedPitchOfBeatClosestToAverage);
                        }
                        else
                        {
                            // Could not determine best candidate. Just take the first one.
                            detectedPitchesOfNote.Add(detectedPitchesOfBeat.FirstOrDefault());
                        }
                    }
                }
            }

            if (!detectedPitchesOfNote.IsNullOrEmpty())
            {
                int medianMidiNote = NumberUtils.Median(detectedPitchesOfNote);
                note.SetMidiNote(medianMidiNote);
            }
        }
        
        if (notify)
        {
            songMetaChangeEventStream.OnNext(new NotesChangedEvent());
        }
    }

    private bool TryFindLocalAveragePitch(List<Note> notes, int beat, int localAverageWindowSizeInBeats, out int localAveragePitch)
    {
        List<Note> notesInWindow = notes
            .Where(note => note.StartBeat - localAverageWindowSizeInBeats <= beat 
                           && beat < note.EndBeat + localAverageWindowSizeInBeats)
            .ToList();
        if (notesInWindow.IsNullOrEmpty())
        {
            localAveragePitch = 0;
            return false;
        }
        
        localAveragePitch = (int)notesInWindow
            .Select(note => note.MidiNote)
            .Average();
        return true;
    }

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
