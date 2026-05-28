using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ForcedAlignmentManager : MonoBehaviour, INeedInjection
{
    public static ForcedAlignmentManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ForcedAlignmentManager>();

    private readonly SemaphoreSlim forcedAlignmentProcessSemaphore = new(1, 1);

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly Subject<ForcedAlignmentFinishedEvent> forcedAlignmentFinishedEventStream = new();
    public Subject<ForcedAlignmentFinishedEvent> ForcedAlignmentFinishedEventStream => forcedAlignmentFinishedEventStream;

    private NemoForcedAligner nemoForcedAligner;
    private string activeModelPath;

    public Job<ForcedAlignmentResult> ProcessSongMetaJob(SongMeta songMeta, ForcedAlignmentInput forcedAlignmentInput)
    {
        Job<ForcedAlignmentResult> job = new Job<ForcedAlignmentResult>(
            Translation.Get(R.Messages.job_forcedAlignmentWithName, "name", Path.GetFileName(songMeta.Audio)),
            new CancellationTokenSource());
        jobManager.AddJob(job);

        job.SetAwaitable(async () =>
        {
            try
            {
                return await ProcessSongMetaAsync(songMeta, job.Progress, forcedAlignmentInput);
            }
            catch (Exception ex)
            {
                ex.Log($"Forced Alignment failed: song '{songMeta.GetArtistDashTitle()}'");
                if (ex is JobAlreadyRunningException)
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
                }
                else
                {
                    NotificationManager.CreateNotification(Translation.Get(Translation.Get(R.Messages.job_forcedAlignment_errorWithReason,
                        "reason", ex.Message)));
                }

                throw ex;
            }
        });
        return job;
    }

    private async Awaitable<ForcedAlignmentResult> ProcessSongMetaAsync(
        SongMeta songMeta,
        JobProgress jobProgress,
        ForcedAlignmentInput forcedAlignmentInput)
    {
        if (forcedAlignmentInput.Lyrics.IsNullOrEmpty())
        {
            return new ForcedAlignmentResult();
        }
        
        // Estimate duration
        int lengthInMillis = (int)Math.Floor((double)forcedAlignmentInput.MonoSamples.Length / forcedAlignmentInput.SampleRate * 1000.0);
        jobProgress.EstimatedCurrentProgressInPercent = (int)Math.Ceiling(lengthInMillis / 2.0);

        ForcedAlignmentResult forcedAlignmentResult = await DoProcessSongMetaAsync(
            songMeta,
            jobProgress.CancellationTokenSource.Token,
            forcedAlignmentInput);

        forcedAlignmentFinishedEventStream.OnNext(new ForcedAlignmentFinishedEvent(songMeta, forcedAlignmentResult));
        return forcedAlignmentResult;
    }

    private async Awaitable<ForcedAlignmentResult> DoProcessSongMetaAsync(SongMeta songMeta,
        CancellationToken cancellationToken,
        ForcedAlignmentInput forcedAlignmentInput)
    {
        // Instant fail if already locked (timeout 0)
        if (!await forcedAlignmentProcessSemaphore.WaitAsync(0, cancellationToken))
        {
            throw new JobAlreadyRunningException(new ForcedAlignmentException("Already performing forced alignment"));
        }

        try
        {
            NemoForcedAlignerConfiguration config = GetNemoForcedAlignerConfiguration();
            if (nemoForcedAligner == null || activeModelPath != config.ModelPath)
            {
                Debug.Log($"Preparing NeMo Forced Aligner (NFA). modelPath: '{config.ModelPath}'");
                nemoForcedAligner = new NemoForcedAligner(config.ModelPath, config.TokensPath);
                activeModelPath = config.ModelPath;
            }

            float[] monoAudioSamplesResampled = AudioSampleUtils.Resample(forcedAlignmentInput.MonoSamples, forcedAlignmentInput.SampleRate, NemoForcedAligner.SampleRate);
            Debug.Log("Resampled mono audio samples for forced alignment: " + monoAudioSamplesResampled.Length);

            NemoForcedAligner.AudioData audioData = new NemoForcedAligner.AudioData
            {
                ChannelCount = 1,
                SampleRate = NemoForcedAligner.SampleRate,
                Samples = monoAudioSamplesResampled,
            };

            // Newline is not a word separator in the forced alignment model.
            string normalizedLyrics = Regex.Replace(forcedAlignmentInput.Lyrics, @"\n", " ");

            await Awaitable.BackgroundThreadAsync();
            NemoForcedAligner.ForcedAlignmentResult nemoForcedAlignmentResult = nemoForcedAligner.Run(audioData, normalizedLyrics);
            Debug.Log($"Forced Alignment finished: {nemoForcedAlignmentResult.Words.Select(w => $"{w.Word}: {w.StartTime:F2} - {w.EndTime:F2}").JoinWith(", ")}");
            NemoForcedAligner.ForcedAlignmentResult paddedNemoForcedAlignmentResult = ToPaddedNemoForcedAlignmentResult(nemoForcedAlignmentResult, audioData);
            await Awaitable.MainThreadAsync();
            
            return ToForcedAlignmentResult(paddedNemoForcedAlignmentResult);
        }
        finally
        {
            forcedAlignmentProcessSemaphore.Release();
        }
    }

    private NemoForcedAligner.ForcedAlignmentResult ToPaddedNemoForcedAlignmentResult(
        NemoForcedAligner.ForcedAlignmentResult nemoForcedAlignmentResult,
        NemoForcedAligner.AudioData audioData
    ) {
        double maxWordLengthForPaddingMs = settings.SongEditorSettings.ForcedAlignmentPaddingMaxWordLengthMs;
        double startPaddingMs = settings.SongEditorSettings.ForcedAlignmentStartPaddingMs;
        double endPaddingMs = settings.SongEditorSettings.ForcedAlignmentEndPaddingMs;
        double audioDurationSec = (double)audioData.Samples.Length / audioData.ChannelCount / audioData.SampleRate;
        double audioDurationMs = audioDurationSec * 1000.0;
        return new WordTimestampPadder(startPaddingMs, endPaddingMs, maxWordLengthForPaddingMs, audioDurationMs)
            .PadTimestamps(nemoForcedAlignmentResult);
    }

    private NemoForcedAlignerConfiguration GetNemoForcedAlignerConfiguration()
    {
        string modelPath = ForcedAlignmentConfigurationUtils.GetModelPath(settings);

        if (!FileUtils.Exists(modelPath))
        {
            throw new FileNotFoundException($"NeMo Forced Aligner ONNX model not found. path: '{modelPath}'", modelPath);
        }

        string tokensPath = modelPath.Replace(".onnx", ".txt");
        if (!FileUtils.Exists(tokensPath))
        {
            throw new FileNotFoundException($"NeMo Forced Aligner tokens file not found. path: '{tokensPath}'", tokensPath);
        }

        return new NemoForcedAlignerConfiguration(modelPath, tokensPath);
    }

    private ForcedAlignmentResult ToForcedAlignmentResult(NemoForcedAligner.ForcedAlignmentResult nemoForcedAlignerResult)
    {
        return new ForcedAlignmentResult
        {
            Words = nemoForcedAlignerResult.Words
                .Where(word => !string.IsNullOrWhiteSpace(word.Word))
                .Select(word => ToForcedAlignmentWordResult(word)).ToList(),
        };
    }

    private WordTimestamp ToForcedAlignmentWordResult(NemoForcedAligner.WordTimestamp word)
    {
        return new WordTimestamp
        {
            Word = word.Word,
            StartTime = word.StartTime,
            EndTime = word.EndTime,
            Tokens = word.Tokens.Select(token => ToForcedAlignmentTokenResult(token)).ToList(),
        };
    }

    private TokenTimestamp ToForcedAlignmentTokenResult(NemoForcedAligner.TokenTimestamp token)
    {
        return new TokenTimestamp
        {
            Token = token.Token,
            StartTime = token.StartTime,
            EndTime = token.EndTime,
        };
    }

    private class NemoForcedAlignerConfiguration
    {
        public string ModelPath { get; set; }
        public string TokensPath { get; set; }

        public NemoForcedAlignerConfiguration(string modelPath, string tokensPath)
        {
            ModelPath = modelPath;
            TokensPath = tokensPath;
        }
    }
}
