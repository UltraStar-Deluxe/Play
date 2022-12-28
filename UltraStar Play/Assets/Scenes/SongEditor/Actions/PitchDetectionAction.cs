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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        lockObject = new();
        pitchDetectionProcessCount = 0;
    }
    private static object lockObject = new();
    private static int pitchDetectionProcessCount;

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

    public void MoveNotesToDetectedPitch(List<Note> notes, bool notify)
    {
        AudioClip audioClip = GetAudioClip(settings.SongEditorSettings.PitchDetectionSamplesSource);
        if (audioClip == null)
        {
            return;
        }

        int minBeat = notes.Select(note => note.StartBeat).Min();
        int maxBeat = notes.Select(note => note.EndBeat).Max();
        int lengthInBeats = maxBeat - minBeat;

        Job pitchDetectionJob = CreateAndAddPitchDetectionJob("Pitch detection to move notes", lengthInBeats);
        DoPitchDetectionAsObservable(minBeat, lengthInBeats)
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

                MoveNotesToPitchDetectionResult(notes, pitchDetectionResult);
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new NotesChangedEvent());
                }
            });
    }

    public void CreateNotesForDetectedPitch(int startBeat, int lengthInBeats, bool notify)
    {
        // Remove old analyzed notes
        songEditorLayerManager.GetEnumLayerNotes(ESongEditorLayer.PitchDetection)
            .Where(oldNote =>
                oldNote.StartBeat >= startBeat && oldNote.EndBeat <= startBeat + lengthInBeats)
            .ForEach(oldNote =>
            {
                editorNoteDisplayer.RemoveNoteControl(oldNote);
                songEditorLayerManager.RemoveNoteFromAllEnumLayers(oldNote);
            });

        Job pitchDetectionJob = CreateAndAddPitchDetectionJob("Pitch detection to create notes", lengthInBeats);
        DoPitchDetectionAsObservable(startBeat, lengthInBeats)
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

                CreateNotesForPitchDetectionResult(pitchDetectionResult);
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new NotesChangedEvent());
                }
            });
    }

    private IObservable<PitchDetectionResult> DoPitchDetectionAsObservable(int startBeat, int lengthInBeats)
    {
        if (pitchDetectionProcessCount > 0)
        {
            uiManager.CreateNotificationVisualElement("Already performing pitch detection");
            return Observable.Throw<PitchDetectionResult>(new IllegalStateException("Already performing pitch detection"));
        }

        AudioClip audioClip = GetAudioClip(settings.SongEditorSettings.PitchDetectionSamplesSource);
        if (audioClip == null)
        {
            return Observable.Throw<PitchDetectionResult>(new IllegalStateException("No AudioClip"));
        }
        int sampleRate = audioClip.frequency;

        float[] audioSamplesForPitchDetection = GetAudioSamplesForPitchDetection(startBeat, lengthInBeats, audioClip);

        // Create audio samples analyzer
        CreateOrUpdateAudioSamplesAnalyzer(audioClip);
        if (audioSamplesAnalyzer == null)
        {
            return null;
        }

        // Do speech recognition in an observable. The observable's code may be executed on a background thread.
        return Observable.Create<PitchDetectionResult>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    pitchDetectionProcessCount++;
                    PitchDetectionResult pitchDetectionResult = new();
                    int endBeatExclusive = startBeat + lengthInBeats;
                    for (int beat = startBeat; beat < endBeatExclusive; beat++)
                    {
                        int offsetInBeats = beat - startBeat;
                        PitchEvent pitchEvent = AnalyzeBeat(audioSamplesForPitchDetection, offsetInBeats, sampleRate);
                        if (pitchEvent == null)
                        {
                            continue;
                        }

                        pitchDetectionResult.Add(beat, pitchEvent.MidiNote);
                    }

                    if (audioSamplesAnalyzer is DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer)
                    {
                        // This is not the common use case of the Dynamic Wavelet algorithm. The next analysis will be independent of the previous one.
                        dywaAudioSamplesAnalyzer.ClearPitchHistory();
                    }

                    o.OnNext(pitchDetectionResult);
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                }
                finally
                {
                    pitchDetectionProcessCount--;
                }

                o.OnCompleted();
                return Disposable.Empty;
            }
        });
    }

    private PitchEvent AnalyzeBeat(float[] samplesMono, int offsetInBeats, int sampleRate)
    {
        // Debug.Log($"Start in ms: {startBeatInMillis}, length in ms: {noteLengthInMillis}, end in ms: {startBeatInMillis + noteLengthInMillis}, start in samples: {startBeatInSamplesMono}, length in samples: {noteLengthInSamplesStereo}, end in samples: {startBeatInSamplesMono + noteLengthInSamplesStereo}");
        // WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-stereo.wav", audioClip.frequency, audioClip.channels, beatSamplesStereo);
        // WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-mono.wav", audioClip.frequency, 1, beatSamplesMono);

        int samplesPerBeat = (int)BpmUtils.GetSamplesPerBeat(songMeta, sampleRate);
        int startIndexInclusive = offsetInBeats * samplesPerBeat;
        int endIndexExclusive = startIndexInclusive + samplesPerBeat;
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(samplesMono, startIndexInclusive, endIndexExclusive, 1, 0);
        return pitchEvent;
    }

    private float[] GetMonoAudioSamples(float[] originalSamples, int channelCount)
    {
        if (channelCount <= 1)
        {
            return originalSamples;
        }

        // Stereo to mono => take the average of the channels
        float[] monoSamples = new float[originalSamples.Length / channelCount];
        int monoSampleIndex = 0;
        for (int stereoSampleIndex = 0; stereoSampleIndex < originalSamples.Length && monoSampleIndex < monoSamples.Length; stereoSampleIndex += channelCount)
        {
            float sampleSum = 0;
            for (int channelIndex = 0; channelIndex < channelCount && (stereoSampleIndex + channelIndex) < originalSamples.Length; channelIndex++)
            {
                sampleSum += originalSamples[stereoSampleIndex + channelIndex];
            }

            float sampleAverage = sampleSum / channelCount;
            monoSamples[monoSampleIndex] = sampleAverage;
            monoSampleIndex++;
        }

        return monoSamples;
    }

    private List<int> AnalyzeBeatsOfNote(Note note, AudioClip audioClip)
    {
        List<int> result = new();
        if (note == null
            || note.Length <= 0)
        {
            return result;
        }

        CreateOrUpdateAudioSamplesAnalyzer(audioClip);
        if (audioSamplesAnalyzer == null)
        {
            return result;
        }

        double beatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double beatLengthInSamples = beatLengthInMillis * audioClip.frequency / 1000;

        float[] audioSamplesOfBeat = new float[(int)beatLengthInSamples];
        for (int beat = note.StartBeat; beat < note.StartBeat + note.Length; beat++)
        {
            double beatStartInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
            int beatStartInSamples = (int) beatStartInMillis * audioClip.frequency / 1000;
            beatStartInSamples = NumberUtils.Limit(beatStartInSamples, 0, audioClip.samples);
            audioClip.GetData(audioSamplesOfBeat, beatStartInSamples);

            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(audioSamplesOfBeat, 0, audioSamplesOfBeat.Length, 1, 0);
            if (pitchEvent != null)
            {
                result.Add(MidiUtils.GetRelativePitch(pitchEvent.MidiNote));
            }
        }
        return result;
    }

    private void CreateOrUpdateAudioSamplesAnalyzer(AudioClip audioClip)
    {
        if ((audioSamplesAnalyzer != null
                && audioSamplesAnalyzerPitchDetectionAlgorithm == settings.SongEditorSettings.PitchDetectionAlgorithm)
            || !songAudioPlayer.HasAudioClip)
        {
            return;
        }

        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(
            settings.SongEditorSettings.PitchDetectionAlgorithm,
            audioClip.frequency);
        audioSamplesAnalyzerPitchDetectionAlgorithm = settings.SongEditorSettings.PitchDetectionAlgorithm;
    }

    private float[] GetAudioSamplesForPitchDetection(int startBeat, int lengthInBeats, AudioClip audioClip)
    {
        using DisposableStopwatch ds = new("GetAudioSamplesForPitchDetection took <ms>");

        if (lengthInBeats <= 0)
        {
            return null;
        }

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, startBeat);
        double singleBeatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;

        float[] monoAudioSamples = AudioUtils.GetAudioSamples(startBeatInMillis, lengthInMillis, audioClip, true);
        return monoAudioSamples;
    }

    private Job CreateAndAddPitchDetectionJob(string name, int lengthInBeats)
    {
        Job job = new(name);

        double lengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta) * lengthInBeats;
        job.EstimatedTotalDurationInMillis = (int)Math.Ceiling(lengthInMillis / 20);

        job.SetStatus(EJobStatus.Running);
        jobManager.AddJob(job);
        return job;
    }

    private void CreateNotesForPitchDetectionResult(PitchDetectionResult pitchDetectionResult)
    {
        if (pitchDetectionResult.IsEmpty)
        {
            return;
        }

        List<Note> createdNotes = new();
        Note lastCreatedNote = null;
        for (int beat = pitchDetectionResult.MinBeat; beat < pitchDetectionResult.MaxBeat; beat++)
        {
            if (!pitchDetectionResult.TryGetMidiNote(beat, out int midiNote))
            {
                continue;
            }

            if (lastCreatedNote != null
                && lastCreatedNote.MidiNote == midiNote)
            {
                // Extend previously generated note to this beat
                lastCreatedNote.SetLength(lastCreatedNote.Length + 1);
            }
            else
            {
                Note createdNote = new(ENoteType.Normal, beat, 1, MidiUtils.GetUltraStarTxtPitch(midiNote),
                    "");
                createdNotes.Add(createdNote);

                lastCreatedNote = createdNote;
            }
        }

        // IsEditable must be set AFTER the notes have been set completely. Otherwise SetLength will not work.
        createdNotes.ForEach(createdNote =>
        {
            createdNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.PitchDetection);
            songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.PitchDetection, createdNote);
        });
    }

    private void MoveNotesToPitchDetectionResult(List<Note> notes, PitchDetectionResult pitchDetectionResult)
    {
        if (pitchDetectionResult.IsEmpty)
        {
            return;
        }

        Dictionary<Note, List<int>> noteToDetectedPitches = new();
        for (int beat = pitchDetectionResult.MinBeat; beat < pitchDetectionResult.MaxBeat; beat++)
        {
            if (!pitchDetectionResult.TryGetMidiNote(beat, out int midiNote))
            {
                continue;
            }

            List<Note> notesAtBeat = notes
                .Where(note => note.StartBeat <= beat && beat <= note.EndBeat)
                .ToList();
            notesAtBeat.ForEach(note =>
            {
                if (!noteToDetectedPitches.ContainsKey(note))
                {
                    noteToDetectedPitches.Add(note, new List<int>());
                }
                noteToDetectedPitches[note].Add(midiNote);
            });
        }

        noteToDetectedPitches.ForEach(entry =>
        {
            Note note = entry.Key;
            List<int> detectedPitches = entry.Value;
            note.SetMidiNote(NumberUtils.MostOccuringEntry(detectedPitches));
        });
    }

    private class PitchDetectionResult
    {
        private readonly Dictionary<int, int> beatToMidiNote = new();

        public int MinBeat { get; private set; }
        public int MaxBeat { get; private set; }

        public bool IsEmpty => beatToMidiNote.Count <= 0;

        public void Add(int beat, int midiNote)
        {
            if (IsEmpty
                || beat < MinBeat)
            {
                MinBeat = beat;
            }
            if (IsEmpty
                || beat > MaxBeat)
            {
                MaxBeat = beat;
            }

            beatToMidiNote[beat] = midiNote;
        }

        public bool TryGetMidiNote(int beat, out int midiNote)
        {
            return beatToMidiNote.TryGetValue(beat, out midiNote);
        }
    }
}
