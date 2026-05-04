using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionManager : MonoBehaviour, INeedInjection
{
    public static PitchDetectionManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<PitchDetectionManager>();

    private readonly SemaphoreSlim pitchDetectionProcessSemaphore = new(1, 1);

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private AudioSampleLoader audioSampleLoader;

    private readonly Subject<PitchDetectionFinishedEvent> pitchDetectionFinishedEventStream = new();
    public Subject<PitchDetectionFinishedEvent> PitchDetectionFinishedEventStream => pitchDetectionFinishedEventStream;

    private RmvpePitchDetector rmvpePitchDetector;

    public Job<PitchDetectionResult> ProcessSongMetaJob(SongMeta songMeta)
    {
        Job<PitchDetectionResult> job = new Job<PitchDetectionResult>(
            Translation.Get(R.Messages.job_pitchDetectionWithName, "name", Path.GetFileName(songMeta.Audio)),
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
                ex.Log($"Pitch Detection failed: song '{songMeta.GetArtistDashTitle()}'");
                if (ex is JobAlreadyRunningException)
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
                }
                else
                {
                    NotificationManager.CreateNotification(Translation.Get(Translation.Get(R.Messages.job_pitchDetection_errorWithReason,
                        "reason", ex.Message)));
                }

                throw ex;
            }
        });
        return job;
    }

    private async Awaitable<PitchDetectionResult> ProcessSongMetaAsync(
        SongMeta songMeta,
        JobProgress jobProgress)
    {
        string vocalsAudioUri = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
        if (!FileUtils.Exists(vocalsAudioUri))
        {
            throw new PitchDetectionException($"Vocals audio for '{Path.GetFileName(songMeta.Audio)}' does not exist at path '{vocalsAudioUri}'");
        }

        // Estimate duration
        AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(vocalsAudioUri);
        int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
        jobProgress.EstimatedCurrentProgressInPercent = (int)Math.Ceiling(lengthInMillis / 3.0);

        PitchDetectionResult pitchDetectionResult = await DoProcessSongMetaAsync(
            songMeta,
            jobProgress.CancellationTokenSource.Token);
        
        pitchDetectionFinishedEventStream.OnNext(new PitchDetectionFinishedEvent(songMeta, pitchDetectionResult));
        return pitchDetectionResult;
    }

    private async Awaitable<PitchDetectionResult> DoProcessSongMetaAsync(SongMeta songMeta, CancellationToken cancellationToken)
    {
        // Instant fail if already locked (timeout 0)
        if (!await pitchDetectionProcessSemaphore.WaitAsync(0, cancellationToken))
        {
            throw new JobAlreadyRunningException(new PitchDetectionException("Already performing pitch detection"));
        }

        try
        {
            if (rmvpePitchDetector == null)
            {
                string modelPath = ApplicationUtils.GetStreamingAssetsPath("AiModels/rmvpe/rmvpe_20231006.onnx");
                Debug.Log($"Preparing RMVPE pitch detection. modelPath: '{modelPath}'");
                rmvpePitchDetector = new RmvpePitchDetector(modelPath);
            }

            string audioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
            Debug.Log($"Loading audio samples for pitch detection on main thread. path: '{audioFilePath}'");
            AudioClip audioClip = await audioSampleLoader.LoadAsAudioClip(audioFilePath);
            float lengthInSeconds = audioClip.length;
            float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, 0, lengthInSeconds * 1000, true);
            float[] monoAudioSamplesResampled = AudioSampleUtils.Resample(monoAudioSamples, audioClip.frequency, RmvpePitchDetector.ExpectedSampleRate);
            Debug.Log("Resampled mono audio samples for pitch detection: " + monoAudioSamplesResampled.Length);

            Debug.Log($"Running RMVPE pitch detection on vocals audio. path: '{songMeta.VocalsAudio}'");
            await Awaitable.BackgroundThreadAsync();
            RmvpePitchResult rmvpePitchResult = rmvpePitchDetector.DetectPitch(monoAudioSamplesResampled);
            await Awaitable.MainThreadAsync();
            return ToPitchDetectionResult(rmvpePitchResult);
        }
        finally
        {
            pitchDetectionProcessSemaphore.Release();
        }
    }

    private PitchDetectionResult ToPitchDetectionResult(RmvpePitchResult rmvpePitchResult)
    {
        if (rmvpePitchResult.Estimates.IsNullOrEmpty())
        {
            return new PitchDetectionResult();
        }

        List<PitchDetectionResultNote> notes = new();
        PitchDetectionResultNote currentNote = null;

        foreach (RmvpePitchEstimate estimate in rmvpePitchResult.Estimates)
        {
            if (estimate.Confidence <= 0 || estimate.Frequency <= 0)
            {
                currentNote = null;
                continue;
            }

            int midiNote = (int)Math.Round(MidiUtils.CalculateMidiNote((float)estimate.Frequency));
            double timeInMillis = estimate.Time * 1000;

            if (currentNote != null && currentNote.MidiNote == midiNote)
            {
                // Update length of current note.
                currentNote.LengthInMillis = timeInMillis - currentNote.StartInMillis;
            }
            else
            {
                // Start a new note
                currentNote = new PitchDetectionResultNote
                {
                    StartInMillis = timeInMillis,
                    MidiNote = midiNote,
                    Confidence = estimate.Confidence,
                    LengthInMillis = 0, // Will be updated by next estimate or at the end
                };
                notes.Add(currentNote);
            }
        }

        // Finalize lengths.
        if (rmvpePitchResult.Estimates.Count > 1)
        {
            double hopSizeInMillis = (rmvpePitchResult.Estimates[1].Time - rmvpePitchResult.Estimates[0].Time) * 1000;
            foreach (PitchDetectionResultNote note in notes)
            {
                note.LengthInMillis += hopSizeInMillis;
            }
        }
        else if (rmvpePitchResult.Estimates.Count == 1)
        {
            // Fallback for a single estimate. HopLength is 160, SampleRate is 16000 => 10ms.
            foreach (PitchDetectionResultNote note in notes)
            {
                note.LengthInMillis = 10;
            }
        }

        return new PitchDetectionResult
        {
            Notes = notes,
        };
    }
}
