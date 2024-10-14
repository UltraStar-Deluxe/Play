using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NHyphenator;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

    public static IObservable<List<Note>> CreateNotesFromSpeechRecognitionAsObservable(
        float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        SpeechRecognitionParameters speechRecognitionParameters,
        Job speechRecognitionJob,
        bool continuous,
        int midiNote,
        SongMeta songMeta,
        int offsetInBeats,
        Hyphenator hyphenator,
        int spaceInMillisBetweenNotes)
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

        return GetOrCreateSpeechRecognizerAsObservable(speechRecognitionParameters, null)
            .SelectMany(speechRecognizer =>
            {
                speechRecognitionJob?.SetStatus(EJobStatus.Running);

                return DoSpeechRecognitionAsObservable(
                        monoAudioSamples,
                        startIndex,
                        endIndex,
                        sampleRate,
                        cancellationTokenSource.Token,
                        onProgress,
                        speechRecognizer,
                        continuous)
                    // Execute on Background thread
                    .SubscribeOn(Scheduler.ThreadPool)
                    // Notify on Main thread
                    .ObserveOnMainThread();
            })
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Create notes from speech recognition failed: {ex.Message}");
                speechRecognitionJob?.SetResult(EJobResult.Error);
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                    "reason", ex.Message));
                throw ex;
            })
            .Select(speechRecognitionResult =>
            {
                speechRecognitionJob?.SetResult(EJobResult.Ok);
                List<Note> createdNotes = speechRecognitionResult != null && !speechRecognitionResult.Words.IsNullOrEmpty()
                    ? CreateNotesFromSpeechRecognitionResult(speechRecognitionResult.Words, songMeta, offsetInBeats, midiNote, hyphenator, spaceInMillisBetweenNotes)
                    : new List<Note>();

                return createdNotes;
            });
    }

    public static IObservable<SpeechRecognizer> GetOrCreateSpeechRecognizerAsObservable(
        SpeechRecognitionParameters parameters,
        Job parentJob)
    {
        SpeechRecognizer existingSpeechRecognizer = SpeechRecognitionManager.Instance.GetExistingSpeechRecognizer(parameters);
        if (existingSpeechRecognizer != null
            && existingSpeechRecognizer.IsLoaded)
        {
            // Nothing to do. Return observable that fires immediately.
            return Observable.Create<SpeechRecognizer>(o =>
            {
                o.OnNext(existingSpeechRecognizer);
                o.OnCompleted();
                return Disposable.Empty;
            });
        }

        // Create UI job
        Job loadSpeechRecognizerJob = new(Translation.Get(R.Messages.job_loadSpeechRecognitionModel), parentJob);
        loadSpeechRecognizerJob.EstimatedTotalDurationInMillis = 60000;
        loadSpeechRecognizerJob.SetStatus(EJobStatus.Running);
        JobManager.Instance.AddJob(loadSpeechRecognizerJob);

        if (!SpeechRecognitionManager.Instance.TryGetOrCreateSpeechRecognizer(
                parameters,
                out string errorMessage,
                out SpeechRecognizer _))
        {
            loadSpeechRecognizerJob.SetResult(EJobResult.Error);
            return Observable.Create<SpeechRecognizer>(o =>
            {
                o.OnError(new Exception(errorMessage));
                o.OnCompleted();
                return Disposable.Empty;
            });
        }

        return LoadSpeechRecognizerAsObservable(parameters)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Load speech recognizer failed: {ex.Message}");
                loadSpeechRecognizerJob.SetResult(EJobResult.Error);
                throw ex;
            })
            .Select(speechRecognizer =>
            {
                loadSpeechRecognizerJob.SetResult(EJobResult.Ok);
                return speechRecognizer;
            });
    }

    private static IObservable<SpeechRecognizer> LoadSpeechRecognizerAsObservable(
        SpeechRecognitionParameters parameters)
    {
        if (speechRecognitionProcessCount > 0)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            return Observable.Throw<SpeechRecognizer>(new IllegalStateException("Already performing speech recognition"));
        }

        SpeechRecognitionManager speechRecognitionManager = SpeechRecognitionManager.Instance;

        return Observable.Create<SpeechRecognizer>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    speechRecognitionProcessCount++;

                    if (!speechRecognitionManager.TryInitExistingSpeechRecognizer(parameters, out string errorMessage))
                    {
                        o.OnError(new IllegalStateException(errorMessage));
                        return Disposable.Empty;
                    }

                    SpeechRecognizer existingSpeechRecognizer = speechRecognitionManager.GetExistingSpeechRecognizer(parameters);
                    o.OnNext(existingSpeechRecognizer);
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
        SpeechRecognizer speechRecognizer,
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

                    Stopwatch stopwatch = Stopwatch.StartNew();

                    SpeechRecognitionResult speechRecognitionResult = speechRecognizer.GetSpeechRecognitionResult(
                        monoSamples,
                        startIndex,
                        endIndex,
                        sampleRate,
                        cancellationToken,
                        onProgress);

                    double startSecond = (double)startIndex / sampleRate;
                    double endSecond = (double)endIndex / sampleRate;
                    Debug.Log($"Analyzed text from second {startSecond:0.00} to second {endSecond:0.00} (duration of {endSecond-startSecond:0.00} seconds). Took {(stopwatch.ElapsedMilliseconds / 1000.0):0.00} seconds. Result: {speechRecognitionResult?.Text}");

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

    private static List<Note> CreateNotesFromSpeechRecognitionResult(
        List<SpeechRecognitionWordResult> words,
        SongMeta songMeta,
        int offsetInBeats,
        int midiNote,
        Hyphenator hyphenator,
        int spaceInMillisBetweenNotes)
    {
        double beatsPerSeconds = SongMetaBpmUtils.BeatsPerSecond(songMeta);
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
        SpaceBetweenNotesUtils.ShortenNotesByMillis(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

        // Split syllables if hyphenation is enabled
        if (hyphenator != null)
        {
            Dictionary<Note,List<Note>> noteToNotesAfterSplit = HyphenateNotesUtils.HypenateNotes(songMeta, createdNotes, hyphenator);
            noteToNotesAfterSplit.ForEach(entry =>
            {
                Note note = entry.Key;
                List<Note> notesAfterSplit = entry.Value;
                List<Note> newNotes = new List<Note>(notesAfterSplit);
                newNotes.Remove(note);
                createdNotes.AddRange(newNotes);
            });
        }

        // Shorten new notes left and right to give a little space
        if (spaceInMillisBetweenNotes > 0)
        {
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, spaceInMillisBetweenNotes, songMeta);
        }

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
                double noteStartInMillis = SongMetaBpmUtils.BeatsToMillisWithoutGap(songMeta, note.StartBeat - wordOffsetInBeats);
                double noteEndInMillis = SongMetaBpmUtils.BeatsToMillisWithoutGap(songMeta, note.EndBeat - wordOffsetInBeats);

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
