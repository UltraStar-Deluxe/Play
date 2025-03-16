using System;
using System.Diagnostics;
using System.Threading;
using UniInject;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionManager : MonoBehaviour, INeedInjection
{
    public static SpeechRecognitionManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SpeechRecognitionManager>();

    [Inject]
    private Settings settings;

    private readonly SemaphoreSlim speechRecognitionProcessSemaphore = new(1, 1);
    public bool IsSpeechRecognitionRunning => speechRecognitionProcessSemaphore.CurrentCount > 0;

    public Job<SpeechRecognitionResult> ProcessSongMetaJob(
        SpeechRecognitionInputSamples samples,
        SpeechRecognizer speechRecognizer)
    {
        double lengthInMillis = ((double)(samples.EndIndex - samples.StartIndex) / samples.SampleRate) * 1000.0;

        Job<SpeechRecognitionResult> job = new(Translation.Get(R.Messages.job_speechRecognition), new CancellationTokenSource());
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
            throw new JobAlreadyRunningException(new SpeechRecognitionException("Already performing speech recognition"));
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

            SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.GetSpeechRecognitionResultAsync(
                samples,
                jobProgress.CancellationTokenSource.Token,
                progressInPercent => jobProgress.EstimatedCurrentProgressInPercent = progressInPercent);

            double startSecond = (double)samples.StartIndex / samples.SampleRate;
            double endSecond = (double)samples.EndIndex / samples.SampleRate;
            Log.Debug(() => $"Analyzed text from second {startSecond:0.00} to second {endSecond:0.00} (duration of {endSecond-startSecond:0.00} seconds). Took {(stopwatch.ElapsedMilliseconds / 1000.0):0.00} seconds. Result: {speechRecognitionResult?.Text}");

            return speechRecognitionResult;
        }
        finally
        {
            speechRecognitionProcessSemaphore.Release();
        }
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
}
