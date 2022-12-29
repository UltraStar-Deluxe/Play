using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    }
    private static object lockObject = new();
    private static int speechRecognitionProcessCount;

    public static int GetEstimatedSpeechRecognitionDurationInMillis(SongMeta songMeta, int lengthInBeats)
    {
        double lengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta) * lengthInBeats;
        return (int)Math.Ceiling(lengthInMillis / 4);
    }

    public static IObservable<List<Note>> CreateNotesFromSpeechRecognition(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats,
        VoskModelParameters voskModelParameters,
        int midiNote,
        Job speechRecognitionJob = null)
    {
        // Create UI job if needed
        if (speechRecognitionJob == null)
        {
            speechRecognitionJob = new("Speech recognition to create notes");
            JobManager.Instance.AddJob(speechRecognitionJob);
        }
        speechRecognitionJob.SetStatus(EJobStatus.Running);
        speechRecognitionJob.EstimatedTotalDurationInMillis = GetEstimatedSpeechRecognitionDurationInMillis(songMeta, lengthInBeats);

        Subject<List<Note>> createNotesFromSpeechRecognitionSubject = new();
        DoSpeechRecognitionAsObservable(songMeta, audioClip, startBeat, lengthInBeats, voskModelParameters)
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
        return createNotesFromSpeechRecognitionSubject;
    }

    public static IObservable<VoskResultJson> DoSpeechRecognitionAsObservable(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats,
        VoskModelParameters speechRecognizerParameters)
    {
        if (speechRecognitionProcessCount > 0)
        {
            UiManager.Instance.CreateNotificationVisualElement("Already performing speech recognition");
            return Observable.Throw<VoskResultJson>(new IllegalStateException("Already performing speech recognition"));
        }

        // AudioClip can only be accessed on the main thread.
        // Thus, read all needed data before creating the observable that will be executed on a background thread.
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
                    VoskRecognizer speechRecognizer = speechRecognitionManager.GetSpeechRecognizer(speechRecognizerParameters);
                    VoskResultJson voskResultJson = AnalyzeSamples(audioSamplesForSpeechRecognition, speechRecognizer);
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

    private static VoskResultJson AnalyzeSamples(short[] monoSamplesArray, VoskRecognizer voskRecognizer)
    {
        using DisposableStopwatch ds = new("Speech recognition took <ms>");

        voskRecognizer.AcceptWaveform(monoSamplesArray, monoSamplesArray.Length);
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
}
