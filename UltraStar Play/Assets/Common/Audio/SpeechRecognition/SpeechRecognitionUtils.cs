using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        speechRecognitionProcessSemaphore = new(1,  1);
        IsApplicationTerminating = false;
    }

    private static SemaphoreSlim speechRecognitionProcessSemaphore = new(1, 1);

    public static bool IsSpeechRecognitionRunning => speechRecognitionProcessSemaphore.CurrentCount > 0;

    public static bool IsApplicationTerminating { get; set; }
    public static bool IsExternalSpeechRecognitionCallRunning { get; private set; }

    public static int GetEstimatedSpeechRecognitionDurationInMillis(double lengthInMillis)
    {
        return (int)Math.Ceiling(lengthInMillis);
    }

    public static async Awaitable<List<Note>> CreateNotesFromSpeechRecognitionAsync(float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        SpeechRecognitionParameters speechRecognitionParameters,
        int midiNote,
        SongMeta songMeta,
        int offsetInBeats,
        Hyphenator hyphenator,
        int spaceInMillisBetweenNotes,
        Job<SpeechRecognizer> createSpeechRecognizerJob = null,
        Job<SpeechRecognitionResult> recognizeSpeechJob = null)
    {
        SpeechRecognizer speechRecognizer = await GetOrCreateSpeechRecognizerInJobAsync(speechRecognitionParameters, createSpeechRecognizerJob);

        await Awaitable.BackgroundThreadAsync();
        SpeechRecognitionResult speechRecognitionResult = await RecognizeSpeechInJobAsync(
            monoAudioSamples,
            startIndex,
            endIndex,
            sampleRate,
            speechRecognizer,
            recognizeSpeechJob);

        await Awaitable.MainThreadAsync();
        List<Note> createdNotes = CreateNotesFromSpeechRecognitionResult(speechRecognitionResult, songMeta, offsetInBeats, midiNote, hyphenator, spaceInMillisBetweenNotes);

        return createdNotes;
    }

    public static async Awaitable<SpeechRecognizer> GetOrCreateSpeechRecognizerInJobAsync(
        SpeechRecognitionParameters parameters,
        Job<SpeechRecognizer> existingJob = null)
    {
        Job<SpeechRecognizer> job = existingJob;
        if (job == null)
        {
            job = new Job<SpeechRecognizer>(Translation.Get(R.Messages.job_loadSpeechRecognitionModel));
            JobManager.Instance.AddJob(job);
        }
        job.SetAwaitable(() => GetOrCreateSpeechRecognizerAsync(parameters));
        job.Progress.EstimatedTotalDurationInMillis = 60000;

        return await job.GetResultAsync();
    }

    private static async Awaitable<SpeechRecognizer> GetOrCreateSpeechRecognizerAsync(
        SpeechRecognitionParameters parameters)
    {
        if (!SpeechRecognitionManager.Instance.TryGetOrCreateSpeechRecognizer(
                parameters,
                out string errorMessage,
                out SpeechRecognizer _))
        {
            throw new SpeechRecognitionException(errorMessage);
        }

        await Awaitable.BackgroundThreadAsync();
        SpeechRecognizer speechRecognizer = await LoadSpeechRecognizerAsync(parameters);
        await Awaitable.MainThreadAsync();
        return speechRecognizer;
    }

    public static async Awaitable<SpeechRecognitionResult> RecognizeSpeechInJobAsync(float[] monoSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        SpeechRecognizer speechRecognizer,
        Job<SpeechRecognitionResult> existingJob = null)
    {
        double lengthInMillis = ((double)(endIndex - startIndex) / sampleRate) * 1000.0;

        Job<SpeechRecognitionResult> job = existingJob;
        if (job == null)
        {
            job = new Job<SpeechRecognitionResult>(Translation.Get(R.Messages.job_speechRecognition), new CancellationTokenSource());
            JobManager.Instance.AddJob(job);
        }
        job.SetAwaitable(() => RecognizeSpeechAsync(monoSamples, startIndex, endIndex, sampleRate, job.Progress, speechRecognizer));
        job.Progress.EstimatedTotalDurationInMillis = GetEstimatedSpeechRecognitionDurationInMillis(lengthInMillis);

        return await job.GetResultAsync();
    }

    private static async Awaitable<SpeechRecognitionResult> RecognizeSpeechAsync(
        float[] monoSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        JobProgress jobProgress,
        SpeechRecognizer speechRecognizer)
    {
        // Instant fail if already locked (timeout 0)
        if (!await speechRecognitionProcessSemaphore.WaitAsync(0, jobProgress.CancellationTokenSource.Token))
        {
            throw new JobAlreadyRunningException(new SpeechRecognitionException("Already performing speech recognition"));
        }

        if (startIndex < 0)
        {
            Debug.LogWarning("Received startIndex < 0. Setting startIndex to 0.");
            startIndex = 0;
        }

        int lengthInSamples = endIndex - startIndex;
        if (lengthInSamples <= 0)
        {
            throw new SpeechRecognitionException("No samples for speech recognition");
        }

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.GetSpeechRecognitionResult(
                monoSamples,
                startIndex,
                endIndex,
                sampleRate,
                jobProgress.CancellationTokenSource.Token,
                progressInPercent => jobProgress.EstimatedCurrentProgressInPercent = progressInPercent);

            double startSecond = (double)startIndex / sampleRate;
            double endSecond = (double)endIndex / sampleRate;
            Log.Debug(() => $"Analyzed text from second {startSecond:0.00} to second {endSecond:0.00} (duration of {endSecond-startSecond:0.00} seconds). Took {(stopwatch.ElapsedMilliseconds / 1000.0):0.00} seconds. Result: {speechRecognitionResult?.Text}");

            return speechRecognitionResult;
        }
        finally
        {
            speechRecognitionProcessSemaphore.Release();
        }
    }

    private static async Awaitable<SpeechRecognizer> LoadSpeechRecognizerAsync(
        SpeechRecognitionParameters parameters)
    {
        // Instant fail if already locked (timeout 0)
        if (!await speechRecognitionProcessSemaphore.WaitAsync(0))
        {
            throw new JobAlreadyRunningException(new SpeechRecognitionException("Already performing speech recognition"));
        }

        SpeechRecognitionManager speechRecognitionManager = SpeechRecognitionManager.Instance;

        try
        {
            if (!speechRecognitionManager.TryInitExistingSpeechRecognizer(parameters, out string errorMessage))
            {
                throw new SpeechRecognitionException(errorMessage);
            }

            SpeechRecognizer existingSpeechRecognizer = speechRecognitionManager.GetExistingSpeechRecognizer(parameters);
            return existingSpeechRecognizer;
        }
        finally
        {
            speechRecognitionProcessSemaphore.Release();
        }
    }

    private static List<Note> CreateNotesFromSpeechRecognitionResult(
        SpeechRecognitionResult speechRecognitionResult,
        SongMeta songMeta,
        int offsetInBeats,
        int midiNote,
        Hyphenator hyphenator,
        int spaceInMillisBetweenNotes)
    {
        if (speechRecognitionResult == null
            || speechRecognitionResult.Words.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        double beatsPerSeconds = SongMetaBpmUtils.BeatsPerSecond(songMeta);
        List<Note> createdNotes = speechRecognitionResult.Words.Select(resultEntry =>
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
