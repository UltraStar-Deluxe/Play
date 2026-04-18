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
        if (!ApplicationUtils.IsSupportedBasicPitchDetectionAudioFormat(Path.GetExtension(vocalsAudioUri)))
        {
            throw new PitchDetectionException(
                $"Pitch Detection using Basic Pitch not supported for this audio file. Requires one of {ApplicationUtils.supportedBasicPitchDetectionAudioFiles.JoinWith(", ")}");
        }

        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);

        // Estimate duration
        AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(vocalsAudioUri);
        int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
        jobProgress.EstimatedCurrentProgressInPercent = (int)Math.Ceiling(lengthInMillis / 3.0);

        // Set path to Basic Pitch executable if needed
        string fallbackPitchDetectionCommand = PlatformUtils.IsWindows
            ? $"\"{ApplicationUtils.GetStreamingAssetsPath("BasicPitchExe/basic_pitch_exe.exe").Replace("/", "\\")}\" --onset_threshold 0.3 --frame_threshold 0.3"
            : "";

        await Awaitable.BackgroundThreadAsync();
        PitchDetectionResult pitchDetectionResult = await DoProcessSongMetaAsync(
            songMeta,
            generatedSongFolderAbsolutePath,
            jobProgress.CancellationTokenSource.Token,
            fallbackPitchDetectionCommand);

        await Awaitable.MainThreadAsync();

        pitchDetectionFinishedEventStream.OnNext(new PitchDetectionFinishedEvent(songMeta, pitchDetectionResult));
        return pitchDetectionResult;
    }

    private async Awaitable<PitchDetectionResult> DoProcessSongMetaAsync(SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        CancellationToken cancellationToken,
        string fallbackCommand)
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

            Debug.Log($"Running RMVPE pitch detection on vocals audio. path: '{songMeta.VocalsAudio}'");
            // TODO: AudioClip API can only be done on the main thread. Use a more flexible library to load the audio samples from file.
            await Awaitable.MainThreadAsync();
            string audioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
            Debug.Log($"Loading audio samples for pitch detection on main thread. path: '{songMeta.VocalsAudio}'");
            AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(audioFilePath, false);
            float lengthInSeconds = audioClip.length;
            float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, 0, lengthInSeconds * 1000, true);
            float[] monoAudioSamplesResampled = AudioSampleUtils.Resample(monoAudioSamples, audioClip.frequency, RmvpePitchDetector.ExpectedSampleRate);
            Debug.Log("Resampled mono audio samples for pitch detection: " + monoAudioSamplesResampled.Length);
            await Awaitable.BackgroundThreadAsync();

            RmvpePitchResult rmvpePitchResult = rmvpePitchDetector.DetectPitch(monoAudioSamplesResampled);
            // TODO: Do not generate wav file when stuff is working as expected.
            RmvpePitchResultAudioGenerator.GenerateWav(rmvpePitchResult.Estimates, ApplicationUtils.GetPersistentDataPath("RmvpePitchDetectionResult.wav"), lengthInSeconds);
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
                // We assume estimates are sorted by time.
                currentNote.LengthInMillis = timeInMillis - currentNote.StartInMillis;
                // Also update confidence as average? Or keep max? 
                // Let's just keep the initial or update if we have multiple.
                // For now, let's just keep it simple.
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

    private bool TryMoveFilesOfBasicPitchResult(
        SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        BasicPitchResult basicPitchResult,
        out string midiFilePath)
    {
        if (basicPitchResult.ExitCode != 0
            || !basicPitchResult.Errors.IsNullOrEmpty())
        {
            throw new PitchDetectionException($"Basic Pitch terminated with exit code {basicPitchResult.ExitCode}. Output:\n{basicPitchResult.Output}");
        }

        if (basicPitchResult.WrittenFiles.IsNullOrEmpty())
        {
            throw new PitchDetectionException($"BasicPitchResult.WrittenFiles is empty. Output:\n{basicPitchResult.Output}");
        }

        // Prepare directory to move created audio files.
        string destinationFolder = DirectoryUtils.IsSubDirectory(SongMetaUtils.GetDirectoryPath(songMeta), generatedSongFolderAbsolutePath)
            ? SongMetaUtils.GetDirectoryPath(songMeta)
            : ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, SongMetaUtils.GetDirectoryPath(songMeta));
        if (!destinationFolder.IsNullOrEmpty()
            && !Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        // Check voice audio
        string writtenMidiFilePath = basicPitchResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileName(filePath).EndsWith(".mid"));
        if (!writtenMidiFilePath.IsNullOrEmpty()
            && File.Exists(writtenMidiFilePath))
        {
            Debug.Log("MIDI file written to: " + writtenMidiFilePath);

            string destinationMidiFilePath = destinationFolder + $"/{Path.GetFileNameWithoutExtension(writtenMidiFilePath)}.mid";
            Debug.Log("Moving MIDI file to: " + destinationMidiFilePath);
            FileUtils.MoveFileOverwriteIfExists(writtenMidiFilePath, destinationMidiFilePath);
            midiFilePath = destinationMidiFilePath;
            return true;
        }
        else
        {
            Debug.LogError($"MIDI file of Basic Pitch not found. Written files: {basicPitchResult.WrittenFiles.JoinWith(", ")}");
            midiFilePath = "";
            return false;
        }
    }

    private string GetBasicPitchCommand(string fallbackCommand)
    {
        return !settings.SongEditorSettings.BasicPitchCommand.IsNullOrEmpty()
            ? settings.SongEditorSettings.BasicPitchCommand
            : fallbackCommand;
    }

    private Action<string> GetBasicPitchLogAction()
    {
        return message => Debug.Log($"BasicPitchRunner: {message}");
    }
}
