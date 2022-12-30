using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UniInject;
using UniRx;
using Vosk;

public static class SpeechRecognitionUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        lockObject = new();
        speechRecognitionProcessCount = 0;
        IsApplicationTerminating = false;
    }
    private static object lockObject = new();
    private static int speechRecognitionProcessCount;

    public static bool IsApplicationTerminating { get; set; }
    public static bool IsExternalSpeechRecognitionProcessRunning { get; private set; }

    public static int GetEstimatedSpeechRecognitionDurationInMillis(SongMeta songMeta, int lengthInBeats)
    {
        double lengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta) * lengthInBeats;
        return (int)Math.Ceiling(lengthInMillis);
    }

    public static IObservable<List<Note>> CreateNotesFromSpeechRecognition(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats,
        SpeechRecognitionParameters speechRecognitionParameters,
        int midiNote,
        Job speechRecognitionJob = null)
    {
        // Create UI job if needed
        if (speechRecognitionJob == null)
        {
            speechRecognitionJob = new("Speech recognition to create notes");
            JobManager.Instance.AddJob(speechRecognitionJob);
        }
        speechRecognitionJob.EstimatedTotalDurationInMillis = GetEstimatedSpeechRecognitionDurationInMillis(songMeta, lengthInBeats);

        CancellationTokenSource cancellationTokenSource = new();
        speechRecognitionJob.OnCancel = () => cancellationTokenSource.Cancel();

        Subject<List<Note>> createNotesFromSpeechRecognitionSubject = new();

        IObservable<object> loadSpeechRecognitionModelObservable = LoadSpeechRecognitionModel(speechRecognitionParameters.ModelPath, null);
        loadSpeechRecognitionModelObservable.Subscribe(_ =>
        {
            speechRecognitionJob.SetStatus(EJobStatus.Running);

            DoSpeechRecognitionAsObservable(
                    songMeta,
                    audioClip,
                    startBeat,
                    lengthInBeats,
                    speechRecognitionParameters,
                    cancellationTokenSource.Token)
                // Execute on Background thread
                .SubscribeOn(Scheduler.ThreadPool)
                // Notify on Main thread
                .ObserveOnMainThread()
                // Handle Exceptions
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogError(ex);
                    speechRecognitionJob.SetResult(EJobResult.Error);
                    createNotesFromSpeechRecognitionSubject.OnError(ex);
                })
                .Subscribe(voskResultJson =>
                {
                    speechRecognitionJob.SetResult(EJobResult.Ok);
                    List<Note> createdNotes = CreateNotesFromVoskResult(voskResultJson.result, songMeta, startBeat, midiNote);

                    createNotesFromSpeechRecognitionSubject.OnNext(createdNotes);
                    createNotesFromSpeechRecognitionSubject.OnCompleted();
                });
        });

        return createNotesFromSpeechRecognitionSubject;
    }

    public static IObservable<object> LoadSpeechRecognitionModel(
        string modelPath,
        Job parentJob)
    {
        if (SpeechRecognitionManager.Instance.HasLoadedSpeechRecognitionModel(modelPath))
        {
            // Nothing to do. Return observable that fires immediately.
            return Observable.Create<object>(o =>
            {
                o.OnNext(true);
                o.OnCompleted();
                return Disposable.Empty;
            });
        }

        // Create UI job
        Job loadSpeechRecognitionModelJob = new("Load speech recognition model", parentJob);
        loadSpeechRecognitionModelJob.EstimatedTotalDurationInMillis = 60000;
        loadSpeechRecognitionModelJob.SetStatus(EJobStatus.Running);
        JobManager.Instance.AddJob(loadSpeechRecognitionModelJob);

        Subject<object> loadSpeechRecognitionModelSubject = new();
        LoadSpeechRecognitionModelAsObservable(modelPath)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                loadSpeechRecognitionModelJob.SetResult(EJobResult.Error);
                loadSpeechRecognitionModelSubject.OnError(ex);
            })
            .Subscribe(_ =>
            {
                loadSpeechRecognitionModelJob.SetResult(EJobResult.Ok);
                loadSpeechRecognitionModelSubject.OnNext(true);
                loadSpeechRecognitionModelSubject.OnCompleted();
            });
        return loadSpeechRecognitionModelSubject;
    }

    private static IObservable<bool> LoadSpeechRecognitionModelAsObservable(
        string modelPath)
    {
        if (speechRecognitionProcessCount > 0)
        {
            UiManager.Instance.CreateNotificationVisualElement("Already performing speech recognition");
            return Observable.Throw<bool>(new IllegalStateException("Already performing speech recognition"));
        }

        SpeechRecognitionManager speechRecognitionManager = SpeechRecognitionManager.Instance;

        return Observable.Create<bool>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    if (!speechRecognitionManager.TryLoadSpeechRecognitionModel(modelPath))
                    {
                        o.OnError(new Exception("Failed to load speech recognition model"));
                        return Disposable.Empty;
                    }

                    o.OnNext(true);
                    o.OnCompleted();

                    return Disposable.Empty;
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                    return Disposable.Empty;
                }
                finally
                {
                    speechRecognitionProcessCount--;
                }
            }
        });
    }

    public static IObservable<VoskResultJson> DoSpeechRecognitionAsObservable(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats,
        SpeechRecognitionParameters speechRecognitionParameters,
        CancellationToken cancellationToken)
    {
        if (speechRecognitionProcessCount > 0)
        {
            UiManager.Instance.CreateNotificationVisualElement("Already performing speech recognition");
            return Observable.Throw<VoskResultJson>(new IllegalStateException("Already performing speech recognition"));
        }

        // AudioClip can only be accessed on the main thread.
        // Thus, read all needed data before creating the observable that will be executed on a background thread.
        int sampleRate = audioClip.frequency;
        short[] audioSamplesForSpeechRecognition = GetAudioSamplesForSpeechRecognition(songMeta, audioClip, startBeat, lengthInBeats);

        SpeechRecognitionManager speechRecognitionManager = SpeechRecognitionManager.Instance;

        // Do speech recognition in an observable. The observable's code may be executed on a background thread.
        return Observable.Create<VoskResultJson>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    speechRecognitionProcessCount++;
                    VoskRecognizer speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);
                    VoskResultJson voskResultJson = AnalyzeSamples(audioSamplesForSpeechRecognition, speechRecognizer, sampleRate, cancellationToken);
                    Debug.Log($"Analyzed text from beat {startBeat} to beat {startBeat + lengthInBeats}. Result: {voskResultJson?.text}");
                    o.OnNext(voskResultJson);
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                    return Disposable.Empty;
                }
                finally
                {
                    speechRecognitionProcessCount--;
                }

                o.OnCompleted();
                return Disposable.Empty;
            }
        });
    }

    private static VoskResultJson AnalyzeSamples(
        short[] monoSamplesArray,
        VoskRecognizer voskRecognizer,
        int sampleRate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using DisposableStopwatch ds = new("Speech recognition took <ms>");

        int targetWindowSizeInSeconds = 3;
        int targetWindowSizeInSamples = sampleRate * targetWindowSizeInSeconds;
        for (int sampleStartIndex = 0; sampleStartIndex < monoSamplesArray.Length; sampleStartIndex += targetWindowSizeInSamples)
        {
            int progressPercent = (int)Math.Floor((double)sampleStartIndex / monoSamplesArray.Length * 100.0);
            if (IsApplicationTerminating)
            {
                throw new Exception($"Exiting speech recognition at {progressPercent} % because application is terminating");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"Canceled speech recognition at {progressPercent} %");
                break;
            }

            // Create array for the samples to be processed in this iteration
            int remainingSampleCount = monoSamplesArray.Length - sampleStartIndex;
            int windowSizeInSamples = remainingSampleCount < targetWindowSizeInSamples
                ? remainingSampleCount
                : targetWindowSizeInSamples;
            short[] windowSamples = new short[windowSizeInSamples];
            Array.Copy(monoSamplesArray, sampleStartIndex, windowSamples, 0, windowSizeInSamples);

            // Process the samples
            try
            {
                IsExternalSpeechRecognitionProcessRunning = true;
                voskRecognizer.AcceptWaveform(windowSamples, windowSamples.Length);
            }
            finally
            {
                IsExternalSpeechRecognitionProcessRunning = false;
            }
        }

        string voskResultJsonString = voskRecognizer.FinalResult();
        Debug.Log($"Raw speech recognition result: {voskResultJsonString}");
        if (voskResultJsonString.IsNullOrEmpty())
        {
            return null;
        }
        VoskResultJson voskResultJson = JsonConverter.FromJson<VoskResultJson>(voskResultJsonString);
        return voskResultJson;
    }

    private static short[] GetAudioSamplesForSpeechRecognition(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats)
    {
        using DisposableStopwatch ds = new("GetAudioSamplesForSpeechRecognition took <ms>");

        if (lengthInBeats <= 0)
        {
            return null;
        }

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, startBeat);
        double singleBeatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;

        float[] monoAudioSamples = AudioUtils.GetAudioSamples(startBeatInMillis, lengthInMillis, audioClip, true);

        // The samples for speech recognition must be a short array.
        // (A float array did not work properly with the used Vosk version)
        short[] monoAudioSamplesAsShorts = AudioUtils.ToShortSampleArray(monoAudioSamples);
        return monoAudioSamplesAsShorts;
    }

    private static List<Note> CreateNotesFromVoskResult(
        List<VoskResultWordJson> voskResultWords,
        SongMeta songMeta,
        int offsetInBeats,
        int midiNote)
    {
        double beatsPerSeconds = BpmUtils.GetBeatsPerSecond(songMeta);
        List<Note> createdNotes = voskResultWords.Select(resultEntry =>
        {
            int noteStartInBeats = offsetInBeats + (int)(resultEntry.start * beatsPerSeconds);
            int noteEndInBeats = offsetInBeats + (int)(resultEntry.end * beatsPerSeconds);
            int noteLengthInBeats = noteEndInBeats - noteStartInBeats;
            string text = resultEntry.word + " ";
            Note createdNote = new(ENoteType.Normal, noteStartInBeats, noteLengthInBeats, MidiUtils.GetUltraStarTxtPitch(midiNote), text);
            return createdNote;
        }).ToList();
        return createdNotes;
    }

    public static List<string> GetSpeechRecognitionPhrases(string lyrics)
    {
        if (lyrics.IsNullOrEmpty()
            || lyrics.Trim().IsNullOrEmpty())
        {
            return new List<string>();
        }

        HashSet<string> wordsHashSet = new();
        string[] words = lyrics.Split(new string[]{" ", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        words.ForEach(word =>
        {
            string normalizedWord = word.Replace("~", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(".", "")
                .Replace("-", "")
                .Trim();
            wordsHashSet.Add(normalizedWord);
        });
        return wordsHashSet.ToList();
    }

    public static void MapSpeechRecognitionResultTextToNotes(SongMeta songMeta, List<VoskResultWordJson> wordsJson, List<Note> notes, int wordOffsetInBeats)
    {
        List<VoskResultWordJson> unusedWordsJson = wordsJson.ToList();
        List<Note> unsetNotes = notes.ToList();

        // First round: Best matching word is the word that has the largest temporal overlap with the note
        unsetNotes.ToList().ForEach(note =>
        {
            VoskResultWordJson bestMatchingWordJson = null;
            double bestMatchingWordOverlapInMillis = 0;
            foreach (VoskResultWordJson wordJson in unusedWordsJson)
            {
                double noteStartInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.StartBeat - wordOffsetInBeats);
                double noteEndInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.EndBeat - wordOffsetInBeats);

                double overlapInMillis = NumberUtils.GetIntersectionLength(
                    noteStartInMillis, noteEndInMillis,
                    wordJson.start * 1000, wordJson.end * 1000);
                if (overlapInMillis > 0
                    && (bestMatchingWordJson == null
                        || bestMatchingWordOverlapInMillis < overlapInMillis))
                {
                    bestMatchingWordJson = wordJson;
                    bestMatchingWordOverlapInMillis = overlapInMillis;
                }
            }

            if (bestMatchingWordJson != null)
            {
                note.SetText(bestMatchingWordJson.word + " ");
                // Do not use this word again
                unusedWordsJson.Remove(bestMatchingWordJson);
                unsetNotes.Remove(note);
            }
        });

        unsetNotes.ForEach(note => note.SetText("_"));
    }
}
