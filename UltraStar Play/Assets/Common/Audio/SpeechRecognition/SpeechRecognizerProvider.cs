using System;
using System.Collections.Generic;
using System.Threading;
using UniInject;
using UnityEngine;

public class SpeechRecognizerProvider : AbstractSingletonBehaviour, INeedInjection
{
    public static SpeechRecognizerProvider Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SpeechRecognizerProvider>();

    private readonly Dictionary<SpeechRecognizerConfig, SpeechRecognizer> parametersToSpeechRecognizer = new();

    private readonly SemaphoreSlim loadSpeechRecognizerSemaphore = new(1, 1);

    protected override object GetInstance()
    {
        return Instance;
    }

    public Job<SpeechRecognizer> GetSpeechRecognizerJob(SpeechRecognizerConfig config)
    {
        Job<SpeechRecognizer> job = new(Translation.Get(R.Messages.job_loadSpeechRecognitionModel));
        JobManager.Instance.AddJob(job);
        job.SetAwaitable(() => GetSpeechRecognizerAsync(config));
        job.Progress.EstimatedTotalDurationInMillis = 60000;

        return job;
    }

    private async Awaitable<SpeechRecognizer> GetSpeechRecognizerAsync(SpeechRecognizerConfig config)
    {
        if (parametersToSpeechRecognizer.TryGetValue(config, out SpeechRecognizer speechRecognizer))
        {
            Log.Debug(() => $"Reusing cached speech recognizer for parameters {config}");
            return speechRecognizer;
        }

        string modelPath = config.ModelPath;
        if (modelPath.IsNullOrEmpty())
        {
            throw new SpeechRecognitionException("Set the speech recognition model path first.");
        }
        if (!FileUtils.Exists(modelPath))
        {
            throw new SpeechRecognitionException($"Speech recognition model path is not a valid file path: '{modelPath}'");
        }

        speechRecognizer = WhisperSpeechRecognizerProvider.Instance.CreateSpeechRecognizer(config);

        await Awaitable.BackgroundThreadAsync();
        await InitSpeechRecognizerAsync(speechRecognizer);
        await Awaitable.MainThreadAsync();

        parametersToSpeechRecognizer[config] = speechRecognizer;
        return speechRecognizer;
    }

    private async Awaitable InitSpeechRecognizerAsync(SpeechRecognizer speechRecognizer)
    {
        if (speechRecognizer.IsLoaded)
        {
            return;
        }

        if (!new WhisperSupportChecker().IsWhisperSupportedOnHardware())
        {
            throw new SpeechRecognitionException("Speech recognition is not yet supported on this hardware.\nPlease wait for a future release.");
        }

        // Instant fail if already locked (timeout 0)
        if (!await loadSpeechRecognizerSemaphore.WaitAsync(0))
        {
            throw new JobAlreadyRunningException(new SpeechRecognitionException("Already performing speech recognition"));
        }

        try
        {
            speechRecognizer.InitModel();
        }
        finally
        {
            loadSpeechRecognizerSemaphore.Release();
        }
    }

    private void OnApplicationQuit()
    {
        SemaphoreUtils.SleepUntilSemaphoreIsFree(
            loadSpeechRecognizerSemaphore,
            "load speech recognizer",
            TimeSpan.FromMilliseconds(5000));
    }
}
