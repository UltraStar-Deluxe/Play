using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UniInject;
using UnityEngine;
using Eitan.Sherpa.Onnx.Unity.Mono.Components;
using Eitan.SherpaONNXUnity.Runtime;
using Eitan.SherpaONNXUnity.Runtime.Modules;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionManager : MonoBehaviour, INeedInjection
{
    private const int ParakeetV3ExpectedSampleRate = 16000;

    public static SpeechRecognitionManager Instance =>
        DontDestroyOnLoadManager.FindComponentOrThrow<SpeechRecognitionManager>();

    [Inject] private Settings settings;

    [InjectedInInspector] public OfflineSpeechRecognizerComponent offlineRecognizer;

    private readonly SemaphoreSlim speechRecognitionProcessSemaphore = new(1, 1);
    public bool IsSpeechRecognitionRunning => speechRecognitionProcessSemaphore.CurrentCount > 0;

    private bool isInitialized;
    
    void OnEnable()
    {
        if (offlineRecognizer != null)
        {
            offlineRecognizer.TranscriptionFailedEvent.AddListener(HandleTranscriptionFailed);
            offlineRecognizer.InitializationStateChangedEvent.AddListener(HandleRecognizerReadyState);
            offlineRecognizer.FeedbackMessages.AddListener(HandleFeedbackMessage);
            offlineRecognizer.FeedbackReceived += HandleFeedback;
        }
    }

    void OnDisable()
    {
        if (offlineRecognizer != null)
        {
            offlineRecognizer.TranscriptionFailedEvent.RemoveListener(HandleTranscriptionFailed);
            offlineRecognizer.InitializationStateChangedEvent.RemoveListener(HandleRecognizerReadyState);
            offlineRecognizer.FeedbackMessages.RemoveListener(HandleFeedbackMessage);
            offlineRecognizer.FeedbackReceived -= HandleFeedback;
        }
    }

    public Job<SpeechRecognitionResult> ProcessSongMetaJob(
        SpeechRecognitionInputSamples samples,
        SpeechRecognizer speechRecognizer)
    {
        double lengthInMillis = ((double)(samples.EndIndex - samples.StartIndex) / samples.SampleRate) * 1000.0;

        Job<SpeechRecognitionResult> job = new(Translation.Get(R.Messages.job_speechRecognition),
            new CancellationTokenSource());
        JobManager.Instance.AddJob(job);
        job.SetAwaitable(() => ProcessSongMetaAsync(samples, speechRecognizer, job.Progress));
        job.Progress.EstimatedTotalDurationInMillis = GetEstimatedSpeechRecognitionDurationInMillis(lengthInMillis);

        return job;
    }

    private async Awaitable<SpeechRecognitionResult> ProcessSongMetaAsync(
        SpeechRecognitionInputSamples samples,
        SpeechRecognizer speechRecognizer,
        JobProgress jobProgress)
    {
        // Instant fail if already locked (timeout 0)
        if (!await speechRecognitionProcessSemaphore.WaitAsync(0, jobProgress.CancellationTokenSource.Token))
        {
            throw new JobAlreadyRunningException(
                new SpeechRecognitionException("Already performing speech recognition"));
        }

        if (samples.StartIndex < 0)
        {
            Debug.LogWarning("Received startIndex < 0. Setting startIndex to 0.");
            samples.StartIndex = 0;
        }

        int lengthInSamples = samples.EndIndex - samples.StartIndex;
        if (lengthInSamples <= 0)
        {
            throw new SpeechRecognitionException("No samples for speech recognition");
        }

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // TODO: Clean up speech recognition code. Remove code for Whisper integration.
            // SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.GetSpeechRecognitionResultAsync(
            //     samples,
            //     jobProgress.CancellationTokenSource.Token,
            //     progressInPercent => jobProgress.EstimatedCurrentProgressInPercent = progressInPercent);

            float[] monoSamplesExcerpt = new float[samples.EndIndex - samples.StartIndex];
            for (int i = 0; i < monoSamplesExcerpt.Length; i++)
            {
                monoSamplesExcerpt[i] = samples.MonoSamples[i + samples.StartIndex];
            }

            Debug.Log("Resample audio for speech recognition");
            WavFileWriter.WriteFile(ApplicationUtils.GetPersistentDataPath("SpeechRecognitionMonoAudioSamplesExcerpt.wav"),samples.SampleRate,1, monoSamplesExcerpt);
            float[] monoSamplesExcerptResampled = AudioSampleUtils.Resample(monoSamplesExcerpt, samples.SampleRate, ParakeetV3ExpectedSampleRate);
            WavFileWriter.WriteFile(ApplicationUtils.GetPersistentDataPath("SpeechRecognitionMonoAudioSamplesExcerptResampled.wav"),ParakeetV3ExpectedSampleRate,1, monoSamplesExcerptResampled);

            var sherpaOnnxResult = await RunSherpaOnnxSpeechRecognitionAsync(
                monoSamplesExcerptResampled,
                ParakeetV3ExpectedSampleRate,
                new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token);
            Debug.Log("Speech recognition complete.");

            if (sherpaOnnxResult.Error != null)
            {
                Debug.LogException(sherpaOnnxResult.Error);
            }
            
            if (sherpaOnnxResult.Status != SpeechRecognition.TranscriptionStatus.Success)
            {
                throw new SpeechRecognitionException($"Speech recognition failed. result: {sherpaOnnxResult.Status}");
            }

            if (string.IsNullOrWhiteSpace(sherpaOnnxResult.Text))
            {
                throw new SpeechRecognitionException("Speech recognition result is empty");
            }

            SpeechRecognitionResult speechRecognitionResult = ToSpeechRecognitionResult(sherpaOnnxResult);

            double startSecond = (double)samples.StartIndex / samples.SampleRate;
            double endSecond = (double)samples.EndIndex / samples.SampleRate;
            Log.Debug(() =>
                $"Analyzed text from second {startSecond:0.00} to second {endSecond:0.00} (duration of {endSecond - startSecond:0.00} seconds). Took {(stopwatch.ElapsedMilliseconds / 1000.0):0.00} seconds. Result: {speechRecognitionResult?.Text}");

            return speechRecognitionResult;
        }
        finally
        {
            speechRecognitionProcessSemaphore.Release();
        }
    }

    private SpeechRecognitionResult ToSpeechRecognitionResult(SpeechRecognition.TranscriptionResult sherpaOnnxResult)
    {
        List<SpeechRecognitionWordResult> wordResults = new();
        for (int i = 0; i < sherpaOnnxResult.Tokens.Length; i++)
        {
            string token = sherpaOnnxResult.Tokens[i];
            float startTimeInSeconds = sherpaOnnxResult.Timestamps[i];
            float lengthInSeconds = sherpaOnnxResult.Durations[i];
            wordResults.Add(new SpeechRecognitionWordResult(
                token,
                TimeSpan.FromSeconds(startTimeInSeconds),
                TimeSpan.FromSeconds(startTimeInSeconds + lengthInSeconds)));
        }

        return new SpeechRecognitionResult(
            sherpaOnnxResult.Text,
            wordResults
        );
    }

    private int GetEstimatedSpeechRecognitionDurationInMillis(double lengthInMillis)
    {
        return (int)Math.Ceiling(lengthInMillis);
    }

    private void OnApplicationQuit()
    {
        SemaphoreUtils.SleepUntilSemaphoreIsFree(
            speechRecognitionProcessSemaphore,
            "speech recognition",
            TimeSpan.FromMilliseconds(5000));
    }

    private async Awaitable<SpeechRecognition.TranscriptionResult> RunSherpaOnnxSpeechRecognitionAsync(
        float[] audioSamples,
        int sampleRate,
        CancellationToken cancellationToken)
    {
        await Awaitable.MainThreadAsync();
        Debug.Log("Creating AudioClip for speech recognition on main thread.");
        AudioClip clip = AudioClip.Create("SpeechRecognitionMonoAudioSamplesClip", audioSamples.Length, 1, sampleRate, false);
        clip.SetData(audioSamples, 0);

        Debug.Log("Running speech recognition");
        SpeechRecognition.TranscriptionResult result = await offlineRecognizer
            .TranscribeClipAsync(clip, cancellationToken).ConfigureAwait(true);
        await Awaitable.BackgroundThreadAsync();
        return result;
    }

    private void HandleTranscriptionFailed(string message)
    {
        Debug.LogError(message);
    }

    private void HandleRecognizerReadyState(bool ready)
    {
        Debug.Log($"HandleRecognizerReadyState: {ready}");
    }

    private void HandleFeedbackMessage(string message)
    {
        Debug.Log($"HandleFeedbackMessage: {message}");
    }

    private void HandleFeedback(SherpaFeedback feedback)
    {
        Debug.Log($"HandleFeedback: {feedback.Message}");
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        try
        {
            _ = offlineRecognizer.StartModuleInitializationAsync();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
