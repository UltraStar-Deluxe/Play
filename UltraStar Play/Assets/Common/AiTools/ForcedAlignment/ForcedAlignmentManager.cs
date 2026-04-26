using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BasicPitchRunner;
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

    public Job<ForcedAlignmentResult> ProcessSongMetaJob(SongMeta songMeta)
    {
        Job<ForcedAlignmentResult> job = new Job<ForcedAlignmentResult>(
            Translation.Get(R.Messages.job_forcedAlignmentWithName, "name", Path.GetFileName(songMeta.Audio)),
            new CancellationTokenSource());
        jobManager.AddJob(job);

        job.SetAwaitable(async () =>
        {
            try
            {
                return await ProcessSongMetaAsync(songMeta, job.Progress);
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
        JobProgress jobProgress)
    {
        string vocalsAudioUri = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
        if (!FileUtils.Exists(vocalsAudioUri))
        {
            throw new ForcedAlignmentException($"Vocals audio for '{Path.GetFileName(songMeta.Audio)}' does not exist at path '{vocalsAudioUri}'");
        }
        
        // Estimate duration
        AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(vocalsAudioUri);
        int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
        jobProgress.EstimatedCurrentProgressInPercent = (int)Math.Ceiling(lengthInMillis / 2.0);

        ForcedAlignmentResult forcedAlignmentResult = await DoProcessSongMetaAsync(
            songMeta,
            jobProgress.CancellationTokenSource.Token);

        forcedAlignmentFinishedEventStream.OnNext(new ForcedAlignmentFinishedEvent(songMeta, forcedAlignmentResult));
        return forcedAlignmentResult;
    }

    private async Awaitable<ForcedAlignmentResult> DoProcessSongMetaAsync(SongMeta songMeta,
        CancellationToken cancellationToken)
    {
        // Instant fail if already locked (timeout 0)
        if (!await forcedAlignmentProcessSemaphore.WaitAsync(0, cancellationToken))
        {
            throw new JobAlreadyRunningException(new ForcedAlignmentException("Already performing forced alignment"));
        }

        try
        {
            if (nemoForcedAligner == null)
            {
                // TODO: Handle different languages
                NemoForcedAligner.Configuration config = GetNemoForcedAlignerConfiguration("en");
                Debug.Log($"Preparing NeMo Forced Aligner (NFA). modelPath: '{config.ModelPath}'");
                nemoForcedAligner = new NemoForcedAligner(config.ModelPath, config.TokensPath);
            }

            Debug.Log($"Running forced alignment on vocals audio. path: '{songMeta.VocalsAudio}'");
            
            // TODO: AudioClip API can only be done on the main thread. Use a more flexible library to load the audio samples from file.
            await Awaitable.MainThreadAsync();
            string audioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
            Debug.Log($"Loading audio samples for pitch detection on main thread. path: '{songMeta.VocalsAudio}'");
            AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(audioFilePath, false);
            float lengthInSeconds = audioClip.length;
            float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, 0, lengthInSeconds * 1000, true);
            float[] monoAudioSamplesResampled = AudioSampleUtils.Resample(monoAudioSamples, audioClip.frequency, NemoForcedAligner.SampleRate);
            Debug.Log("Resampled mono audio samples for forced alignment: " + monoAudioSamplesResampled.Length);
            await Awaitable.BackgroundThreadAsync();

            NemoForcedAligner.AudioData audioData = new NemoForcedAligner.AudioData
            {
                ChannelCount = 1,
                SampleRate = NemoForcedAligner.SampleRate,
                Samples = monoAudioSamplesResampled,
            };
            
            string lyrics = SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1, true)
                .Replace("\n", " ");
            
            NemoForcedAligner.ForcedAlignmentResult nemoForcedAlignmentResult = nemoForcedAligner.Run(audioData, lyrics);

            Debug.Log($"Forced Alignment finished: {nemoForcedAlignmentResult.Words.Select(w => $"{w.Word}: {w.StartTime:F2} - {w.EndTime:F2}").JoinWith(", ")}");
            
            return ToForcedAlignmentResult(nemoForcedAlignmentResult);
        }
        finally
        {
            forcedAlignmentProcessSemaphore.Release();
        }
    }

    private NemoForcedAligner.Configuration GetNemoForcedAlignerConfiguration(string language)
    {
        string modelName = "stt_en_conformer_ctc_small";
        if (language == "en")
        {
            modelName = "stt_de_conformer_ctc_large";
        }
        else if (language == "es")
        {
            modelName = "stt_es_conformer_ctc_large";
        }

        return new NemoForcedAligner.Configuration(
            ApplicationUtils.GetStreamingAssetsPath($"AiModels/NeMoForcedAligner/{modelName}.onnx"),
            ApplicationUtils.GetStreamingAssetsPath($"AiModels/NeMoForcedAligner/tokens_{modelName}.txt"));
    }

    private ForcedAlignmentResult ToForcedAlignmentResult(NemoForcedAligner.ForcedAlignmentResult nemoForcedAlignerResult)
    {
        return new ForcedAlignmentResult
        {
            Tokens = nemoForcedAlignerResult.Tokens.Select(token => ToForcedAlignmentTokenResult(token)).ToList(),
            Words = nemoForcedAlignerResult.Words.Select(word => ToForcedAlignmentWordResult(word)).ToList(),
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
}
