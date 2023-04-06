using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class PitchDetectionUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        lockObject = new();
        pitchDetectionProcessCount = 0;
    }
    private static object lockObject = new();
    private static int pitchDetectionProcessCount;

    public static long GetEstimatedPitchDetectionDurationInMillis(SongMeta songMeta, int lengthInBeats)
    {
        double lengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta) * lengthInBeats;
        return (int)Math.Ceiling(lengthInMillis / 20);
    }

    public static IObservable<bool> MoveNotesToDetectedPitch(
        SongMeta songMeta,
        List<Note> notes,
        AudioClip audioClip,
        EPitchDetectionAlgorithm pitchDetectionAlgorithm,
        Job pitchDetectionJob = null)
    {
        int minBeat = notes.Select(note => note.StartBeat).Min();
        int maxBeat = notes.Select(note => note.EndBeat).Max();
        int lengthInBeats = maxBeat - minBeat;

        if (pitchDetectionJob == null)
        {
            pitchDetectionJob = new Job("Pitch detection to move notes");
            JobManager.Instance.AddJob(pitchDetectionJob);
        }
        pitchDetectionJob.SetStatus(EJobStatus.Running);
        pitchDetectionJob.EstimatedTotalDurationInMillis = GetEstimatedPitchDetectionDurationInMillis(songMeta, lengthInBeats);

        Subject<bool> moveNotesToDetectedPitchSubject = new();
        DoPitchDetectionAsObservable(songMeta, audioClip, minBeat, lengthInBeats, pitchDetectionAlgorithm)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                pitchDetectionJob.SetResult(EJobResult.Error);
                moveNotesToDetectedPitchSubject.OnError(ex);
            })
            .Subscribe(pitchDetectionResult =>
            {
                MoveNotesToPitchDetectionResult(notes, pitchDetectionResult);

                pitchDetectionJob.SetResult(EJobResult.Ok);
                moveNotesToDetectedPitchSubject.OnNext(true);
                moveNotesToDetectedPitchSubject.OnCompleted();
            });

        return moveNotesToDetectedPitchSubject;
    }

    public static IObservable<PitchDetectionResult> DoPitchDetectionAsObservable(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats,
        EPitchDetectionAlgorithm pitchDetectionAlgorithm)
    {
        if (pitchDetectionProcessCount > 0)
        {
            UiManager.CreateNotification("Already performing pitch detection");
            return Observable.Throw<PitchDetectionResult>(new IllegalStateException("Already performing pitch detection"));
        }

        float[] audioSamplesForPitchDetection = GetAudioSamplesForPitchDetection(
            songMeta,
            audioClip,
            startBeat,
            lengthInBeats);
        int sampleRate = audioClip.frequency;

        // Create audio samples analyzer
        IAudioSamplesAnalyzer audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(
            pitchDetectionAlgorithm,
            audioClip.frequency);
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
                    
                    int singlePitchDetectionLengthInMillis = 100;
                    int singlePitchDetectionLengthInBeats = (int)BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, singlePitchDetectionLengthInMillis);
                    singlePitchDetectionLengthInBeats = NumberUtils.Limit(singlePitchDetectionLengthInBeats, 1, int.MaxValue);
                    
                    for (int beat = startBeat; beat < endBeatExclusive; beat += singlePitchDetectionLengthInBeats)
                    {
                        int offsetInBeats = beat - startBeat;
                        PitchEvent pitchEvent = AnalyzeBeats(
                            songMeta,
                            audioSamplesForPitchDetection,
                            offsetInBeats,
                            singlePitchDetectionLengthInBeats,
                            sampleRate,
                            audioSamplesAnalyzer);
                        if (pitchEvent == null)
                        {
                            continue;
                        }

                        pitchDetectionResult.AddRange(beat, lengthInBeats, pitchEvent.MidiNote);
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

    private static PitchEvent AnalyzeBeats(
        SongMeta songMeta,
        float[] samplesMono,
        int sampleOffsetInBeats,
        int lengthInBeats,
        int sampleRate,
        IAudioSamplesAnalyzer audioSamplesAnalyzer)
    {
        int samplesPerBeat = (int)BpmUtils.GetSamplesPerBeat(songMeta, sampleRate);
        int lengthInSamples = samplesPerBeat * lengthInBeats;
        int startIndexInclusive = sampleOffsetInBeats * samplesPerBeat;
        int endIndexExclusive = startIndexInclusive + lengthInSamples;
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(samplesMono, startIndexInclusive, endIndexExclusive, 1, 0);
        return pitchEvent;
    }

    private static float[] GetAudioSamplesForPitchDetection(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats)
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

    public static List<Note> CreateNotesForPitchDetectionResult(PitchDetectionResult pitchDetectionResult)
    {
        if (pitchDetectionResult.IsEmpty)
        {
            return new();
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

        return createdNotes;
    }

    public static void MoveNotesToPitchDetectionResult(List<Note> notes, PitchDetectionResult pitchDetectionResult)
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
}
