using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UnityEngine;
using Vosk;
using Whisper;

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
    public static bool IsSpeechRecognitionRunning => speechRecognitionProcessCount > 0;

    public static bool IsApplicationTerminating { get; set; }
    public static bool IsExternalSpeechRecognitionCallRunning { get; private set; }

    public static int GetEstimatedSpeechRecognitionDurationInMillis(double lengthInMillis)
    {
        return (int)Math.Ceiling(lengthInMillis);
    }

    public static IObservable<List<Note>> CreateNotesFromSpeechRecognition(
        float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        SpeechRecognitionParameters speechRecognitionParameters,
        Job speechRecognitionJob,
        WhisperManager whisperManager,
        bool continuous,
        int midiNote,
        SongMeta songMeta,
        int offsetInBeats)
    {
        CancellationTokenSource cancellationTokenSource = new();
        Action<double> onProgress;

        // Create UI job if needed
        if (speechRecognitionJob != null)
        {
            int lengthInMillis = ((endIndex - startIndex) / sampleRate) * 1000;
            speechRecognitionJob.EstimatedTotalDurationInMillis = GetEstimatedSpeechRecognitionDurationInMillis(lengthInMillis);
            speechRecognitionJob.OnCancel = () => cancellationTokenSource.Cancel();
            onProgress = progressInPercent => speechRecognitionJob.EstimatedCurrentProgressInPercent = progressInPercent;
        }
        else
        {
            onProgress = null;
        }

        Subject<List<Note>> createNotesFromSpeechRecognitionSubject = new();

        IObservable<object> loadSpeechRecognitionModelObservable = LoadSpeechRecognitionModel(speechRecognitionParameters.ModelPath, null);
        loadSpeechRecognitionModelObservable
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                speechRecognitionJob?.SetResult(EJobResult.Error);
                createNotesFromSpeechRecognitionSubject.OnError(ex);
                UiManager.CreateNotification(ex.Message);
            })
            .Subscribe(_ =>
            {
                speechRecognitionJob?.SetStatus(EJobStatus.Running);
                
                DoSpeechRecognitionAsObservable(
                        monoAudioSamples,
                        startIndex,
                        endIndex,
                        sampleRate,
                        cancellationTokenSource.Token,
                        onProgress,
                        whisperManager,
                        continuous)
                    // Execute on Background thread
                    .SubscribeOn(Scheduler.ThreadPool)
                    // Notify on Main thread
                    .ObserveOnMainThread()
                    // Handle Exceptions
                    .CatchIgnore((Exception ex) =>
                    {
                        Debug.LogError(ex);
                        speechRecognitionJob?.SetResult(EJobResult.Error);
                        createNotesFromSpeechRecognitionSubject.OnError(ex);
                        UiManager.CreateNotification(ex.Message);
                    })
                    .Subscribe(speechRecognitionResult =>
                    {
                        speechRecognitionJob?.SetResult(EJobResult.Ok);
                        List<Note> createdNotes = speechRecognitionResult != null && !speechRecognitionResult.Words.IsNullOrEmpty()
                            ? CreateNotesFromSpeechRecognitionResult(speechRecognitionResult.Words, songMeta, offsetInBeats, midiNote)
                            : new List<Note>();

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
            UiManager.CreateNotification("Already performing speech recognition");
            return Observable.Throw<bool>(new IllegalStateException("Already performing speech recognition"));
        }

        SpeechRecognitionManager speechRecognitionManager = SpeechRecognitionManager.Instance;

        return Observable.Create<bool>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    speechRecognitionProcessCount++;

                    if (!speechRecognitionManager.TryLoadSpeechRecognitionModel(modelPath, out string errorMessage))
                    {
                        o.OnError(new IllegalStateException(errorMessage));
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

    public static IObservable<SpeechRecognitionResult> DoSpeechRecognitionAsObservable(
        float[] monoSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        CancellationToken cancellationToken,
        Action<double> onProgress,
        VoskRecognizer speechRecognizer,
        bool continuous)
    {
        if (speechRecognitionProcessCount > 0)
        {
            UiManager.CreateNotification("Already performing speech recognition");
            return Observable.Throw<SpeechRecognitionResult>(new IllegalStateException("Already performing speech recognition"));
        }
        
        // Do speech recognition in an observable. The observable's code may be executed on a background thread.
        return Observable.Create<SpeechRecognitionResult>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    speechRecognitionProcessCount++;
                    
                    short[] audioSamplesForSpeechRecognition = AudioUtils.ToShortSampleArray(monoSamples, startIndex, endIndex);
                    VoskResultJson voskResultJson = AnalyzeSamplesUsingVosk(audioSamplesForSpeechRecognition, speechRecognizer, sampleRate, cancellationToken, onProgress, continuous);

                    SpeechRecognitionResult speechRecognitionResult;
                    if (voskResultJson != null
                        && !voskResultJson.result.IsNullOrEmpty())
                    {
                        List<SpeechRecognitionWordResult> wordResults = voskResultJson.result
                            .Select(voskResultWordJson => new SpeechRecognitionWordResult(
                                    voskResultWordJson.word,
                                    TimeSpan.FromSeconds(voskResultWordJson.start),
                                    TimeSpan.FromSeconds(voskResultWordJson.end)))
                            .ToList();
                        speechRecognitionResult = new SpeechRecognitionResult(voskResultJson.text, wordResults);
                    }
                    else
                    {
                        speechRecognitionResult = null;
                    }

                    double startSecond = (double)startIndex / sampleRate;
                    double endSecond = (double)endIndex / sampleRate;
                    Debug.Log($"Analyzed text from second {startSecond:0.00} to second {endSecond:0.00}. Result: {speechRecognitionResult?.Text}");
                    
                    o.OnNext(speechRecognitionResult);
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
    
    public static IObservable<SpeechRecognitionResult> DoSpeechRecognitionAsObservable(
        float[] monoSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        CancellationToken cancellationToken,
        Action<double> onProgress,
        WhisperManager whisperManager,
        bool continuous)
    {
        if (speechRecognitionProcessCount > 0)
        {
            return Observable.Throw<SpeechRecognitionResult>(new IllegalStateException("Already performing speech recognition"));
        }

        if (startIndex < 0)
        {
            Debug.LogWarning("Received startIndex < 0. Setting startIndex to 0.");
            startIndex = 0;
        }
        
        int lengthInSamples = endIndex - startIndex;
        if (lengthInSamples <= 0)
        {
            return Observable.Throw<SpeechRecognitionResult>(new IllegalStateException("No samples for speech recognition"));
        }
        
        // Do speech recognition in an observable. The observable's code may be executed on a background thread.
        return Observable.Create<SpeechRecognitionResult>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    speechRecognitionProcessCount++;

                    float[] audioSamplesForSpeechRecognition = new float[lengthInSamples];
                    Array.Copy(monoSamples, startIndex, audioSamplesForSpeechRecognition, 0, lengthInSamples);
                    WhisperResult whisperResult = whisperManager.GetTextAsync(audioSamplesForSpeechRecognition, sampleRate, 1)
                        .Result;

                    cancellationToken.ThrowIfCancellationRequested();
                    
                    SpeechRecognitionResult speechRecognitionResult;
                    if (whisperResult != null
                        && !whisperResult.Segments.IsNullOrEmpty()
                        && !whisperResult.Result.StartsWith("[BLANK_AUDIO]", StringComparison.InvariantCultureIgnoreCase)
                        && !whisperResult.Result.StartsWith("[Music]", StringComparison.InvariantCultureIgnoreCase))
                    {
                        TimeSpan offsetToStartIndex = TimeSpan.FromSeconds((double)startIndex / sampleRate);
                        string textResult = whisperResult.Result;
                        List<SpeechRecognitionWordResult> wordResults = whisperResult.Segments
                            .SelectMany(whisperSegment => whisperSegment.Tokens)
                            .Where(token => !token.IsSpecial && !token.Text.TrimStart().StartsWith("[") && !token.Text.TrimEnd().EndsWith("]"))
                            .Select(token => new SpeechRecognitionWordResult(
                                token.Text,
                                token.Timestamp.Start + offsetToStartIndex,
                                token.Timestamp.End + offsetToStartIndex))
                            .ToList();
                        SpeechRecognitionWordResult.NormalizeText(wordResults);
                        wordResults = wordResults
                            .Where(wordResult => !StringUtils.IsOnlyWhitespace(wordResult.Text))
                            .ToList();
                        Debug.Log($"Speech recognition result words: {wordResults.Select(it => $"'{it.Text}'").JoinWith("|")}");
                        speechRecognitionResult = new(textResult, wordResults);
                    }
                    else
                    {
                        speechRecognitionResult = null;
                    }

                    double startSecond = (double)startIndex / sampleRate;
                    double endSecond = (double)endIndex / sampleRate;
                    Debug.Log($"Analyzed text from second {startSecond:0.00} to second {endSecond:0.00}. Result: {speechRecognitionResult?.Text}");
                    
                    o.OnNext(speechRecognitionResult);
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
    
    private static VoskResultJson AnalyzeSamplesUsingVosk(
        short[] monoSamplesArray,
        VoskRecognizer speechRecognizer,
        int sampleRate,
        CancellationToken cancellationToken,
        Action<double> onProgress,
        bool continuous)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using DisposableStopwatch ds = new("Speech recognition took <ms>");
        
        int targetWindowSizeInSeconds = 3;
        int targetWindowSizeInSamples = sampleRate * targetWindowSizeInSeconds;
        for (int sampleStartIndex = 0; sampleStartIndex < monoSamplesArray.Length; sampleStartIndex += targetWindowSizeInSamples)
        {
            double progressPercent = (double)sampleStartIndex / monoSamplesArray.Length * 100.0;
            if (IsApplicationTerminating)
            {
                throw new Exception($"Exiting speech recognition at {(int)progressPercent} % because application is terminating");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"Canceled speech recognition at {(int)progressPercent} %");
                cancellationToken.ThrowIfCancellationRequested();
            }

            onProgress?.Invoke(progressPercent);

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
                IsExternalSpeechRecognitionCallRunning = true;
                speechRecognizer.AcceptWaveform(windowSamples, windowSamples.Length);
            }
            finally
            {
                IsExternalSpeechRecognitionCallRunning = false;
            }
        }

        string voskResultJsonString = continuous
            ? speechRecognizer.Result()
            : speechRecognizer.FinalResult();
        Debug.Log($"Raw speech recognition result: {voskResultJsonString}");
        if (voskResultJsonString.IsNullOrEmpty())
        {
            return null;
        }
        VoskResultJson voskResultJson = JsonConverter.FromJson<VoskResultJson>(voskResultJsonString);
        
        if (!continuous)
        {
            speechRecognizer.Dispose();
        }

        return voskResultJson;
    }

    private static List<Note> CreateNotesFromSpeechRecognitionResult(
        List<SpeechRecognitionWordResult> words,
        SongMeta songMeta,
        int offsetInBeats,
        int midiNote)
    {
        double beatsPerSeconds = BpmUtils.GetBeatsPerSecond(songMeta);
        List<Note> createdNotes = words.Select(resultEntry =>
        {
            int noteStartInBeats = offsetInBeats + (int)(resultEntry.Start.TotalSeconds * beatsPerSeconds);
            int noteEndInBeats = offsetInBeats + (int)(resultEntry.End.TotalSeconds * beatsPerSeconds);
            if (noteEndInBeats <= noteStartInBeats)
            {
                noteEndInBeats = noteStartInBeats + 1;
            }
            int noteLengthInBeats = noteEndInBeats - noteStartInBeats;
            string text = resultEntry.Text;
            Note createdNote = new(ENoteType.Normal, noteStartInBeats, noteLengthInBeats, MidiUtils.GetUltraStarTxtPitch(midiNote), text);
            return createdNote;
        }).ToList();
        
        // Shorten new notes left and right to give a little space
        AddSpaceBetweenNotesUtils.ShortenNotesByMillis(createdNotes, 150, songMeta);
        
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

    public static void MapSpeechRecognitionResultTextToNotes(SongMeta songMeta, List<SpeechRecognitionWordResult> words, List<Note> notes, int wordOffsetInBeats)
    {
        List<SpeechRecognitionWordResult> unusedWords = words.ToList();
        List<Note> unsetNotes = notes.ToList();

        // First round: Best matching word is the word that has the largest temporal overlap with the note
        unsetNotes.ToList().ForEach(note =>
        {
            SpeechRecognitionWordResult bestMatchingWord = null;
            double bestMatchingWordOverlapInMillis = 0;
            foreach (SpeechRecognitionWordResult word in unusedWords)
            {
                double noteStartInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.StartBeat - wordOffsetInBeats);
                double noteEndInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.EndBeat - wordOffsetInBeats);

                double overlapInMillis = NumberUtils.GetIntersectionLength(
                    noteStartInMillis, noteEndInMillis,
                    word.Start.TotalMilliseconds, word.End.TotalMilliseconds);
                if (overlapInMillis > 0
                    && (bestMatchingWord == null
                        || bestMatchingWordOverlapInMillis < overlapInMillis))
                {
                    bestMatchingWord = word;
                    bestMatchingWordOverlapInMillis = overlapInMillis;
                }
            }

            if (bestMatchingWord != null)
            {
                note.SetText(bestMatchingWord.Text + " ");
                // Do not use this word again
                unusedWords.Remove(bestMatchingWord);
                unsetNotes.Remove(note);
            }
        });

        unsetNotes.ForEach(note => note.SetText("_"));
    }
}
