using System;
using System.IO;
using System.Linq;
using System.Threading;
using SpleeterRunner;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AudioSeparationManager : MonoBehaviour, INeedInjection
{
    public static AudioSeparationManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<AudioSeparationManager>();

    private readonly SemaphoreSlim audioSeparationProcessSemaphore = new(1, 1);

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly Subject<AudioSeparationFinishedEvent> audioSeparationFinishedEventStream = new();
    public IObservable<AudioSeparationFinishedEvent> AudioSeparationFinishedEventStream => audioSeparationFinishedEventStream
        .ObserveOnMainThread();

    public Job<AudioSeparationResult> ProcessSongMetaJob(
        SongMeta songMeta,
        bool saveSong)
    {
        Job<AudioSeparationResult> job = new Job<AudioSeparationResult>(
            Translation.Get(R.Messages.job_audioSeparationWithName, "name", Path.GetFileName(songMeta.Audio)),
            new CancellationTokenSource());
        jobManager.AddJob(job);

        job.SetAwaitable(async () =>
        {
            try
            {
                return await ProcessSongMetaAsync(songMeta, saveSong, job.Progress);
            }
            catch (Exception ex)
            {
                ex.Log($"Vocals isolation failed: song '{songMeta.GetArtistDashTitle()}'");
                if (ex is JobAlreadyRunningException)
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
                }
                else
                {
                    NotificationManager.CreateNotification(Translation.Get(Translation.Get(R.Messages.job_audioSeparation_errorWithReason,
                        "reason", ex.Message)));
                }

                throw ex;
            }
        });

        return job;
    }

    private async Awaitable<AudioSeparationResult> ProcessSongMetaAsync(
        SongMeta songMeta,
        bool saveSong,
        JobProgress jobProgress)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);

        // Estimate duration
        if (ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(audioUri)))
        {
            AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(audioUri, false);
            int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
            jobProgress.EstimatedTotalDurationInMillis = (int)Math.Ceiling(lengthInMillis / 2.0);
        }

        // Set path to Spleeter executable if needed
        string fallbackAudioSeparationCommand = PlatformUtils.IsWindows
            ? $"\"{ApplicationUtils.GetStreamingAssetsPath("SpleeterMsvcExe/Spleeter.exe").Replace("/", "\\")}\""
            : "";

        string fileExtension = Path.GetExtension(new Uri(audioUri).LocalPath);
        if (!ApplicationUtils.IsSupportedVocalsSeparationAudioFormat(fileExtension))
        {
            throw new AudioSeparationException($"Vocals isolation not supported for this audio file. Requires one of {ApplicationUtils.supportedVocalsSeparationAudioFiles.JoinWith(", ")}");
        }

        await Awaitable.BackgroundThreadAsync();
        AudioSeparationResult audioSeparationResult = await ProcessSongMetaWithSpleeterAsync(
            songMeta,
            generatedSongFolderAbsolutePath,
            jobProgress.CancellationTokenSource.Token,
            fallbackAudioSeparationCommand,
            saveSong);

        await Awaitable.MainThreadAsync();
        audioSeparationFinishedEventStream.OnNext(new AudioSeparationFinishedEvent(songMeta));
        return audioSeparationResult;
    }

    private async Awaitable<AudioSeparationResult> ProcessSongMetaWithSpleeterAsync(
        SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        CancellationToken cancellationToken,
        string fallbackAudioSeparationCommand,
        bool saveSong)
    {
        // Instant fail if already locked (timeout 0)
        if (!await audioSeparationProcessSemaphore.WaitAsync(0, cancellationToken))
        {
            throw new JobAlreadyRunningException(new AudioSeparationException("Already performing vocals isolation"));
        }

        try
        {
            Debug.Log($"Separating voice and instrumental audio from song: {songMeta}");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(songMeta.Audio);

            SpleeterParameters spleeterParameters = new();
            spleeterParameters.InputFile = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio);
            spleeterParameters.OutputFolder = $"{generatedSongFolderAbsolutePath}/{fileNameWithoutExtension}.ogg";
            spleeterParameters.Overwrite = true;

            Debug.Log($"Calling SpleeterRunner with parameters {JsonConverter.ToJson(spleeterParameters)}");
            SpleeterRunner.SpleeterRunner spleeterRunner = new(GetSpleeterCommand(fallbackAudioSeparationCommand), GetLogAction());
            SpleeterResult spleeterResult = await spleeterRunner.RunAsync(spleeterParameters, cancellationToken);
            Debug.Log($"Call to SpleeterRunner finished: ExitCode={spleeterResult.ExitCode}");

            UpdateSongMetaWithSpleeterResult(songMeta, generatedSongFolderAbsolutePath, spleeterResult, saveSong);

            string originalAudioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio);
            string vocalsAudioFilePath = songMeta.VocalsAudio;
            string instrumentalAudioFilePath = songMeta.InstrumentalAudio;
            return new AudioSeparationResult(originalAudioFilePath, vocalsAudioFilePath, instrumentalAudioFilePath);
        }
        finally
        {
            audioSeparationProcessSemaphore.Release();
        }
    }

    private void UpdateSongMetaWithSpleeterResult(
        SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        SpleeterResult spleeterResult,
        bool saveSong)
    {
        if (spleeterResult.ExitCode != 0
            || !spleeterResult.Errors.IsNullOrEmpty())
        {
            Debug.LogError($"Spleeter terminated with exit code {spleeterResult.ExitCode}. Error messages:\n" +
                           $"    - {spleeterResult.Errors.JoinWith("\n    - ")}");
            return;
        }

        if (spleeterResult.WrittenFiles.IsNullOrEmpty())
        {
            Debug.LogError("SpleeterResult.WrittenFiles is empty");
            return;
        }

        // Save the SongMeta if it changed
        bool songMetaChanged = false;

        // Prepare directory to move created audio files.
        string destinationFolder = SongMetaUtils.GetDirectoryPath(songMeta);
        if (!settings.SaveVocalsAndInstrumentalAudioInFolderOfSong
            && !DirectoryUtils.IsSubDirectory(SongMetaUtils.GetDirectoryPath(songMeta), generatedSongFolderAbsolutePath))
        {
            destinationFolder = ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, SongMetaUtils.GetDirectoryPath(songMeta));
        }

        if (!destinationFolder.IsNullOrEmpty()
            && !Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        // Check voice audio
        string vocalsAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileName(filePath).Contains(".vocals."));
        if (!vocalsAudioPath.IsNullOrEmpty()
            && File.Exists(vocalsAudioPath))
        {
            Debug.Log("Voice audio written to: " + vocalsAudioPath);

            string destinationVocalsAudioPath = destinationFolder + $"/vocals.ogg";
            Debug.Log("Moving voice audio to: " + destinationVocalsAudioPath);
            FileUtils.MoveFileOverwriteIfExists(vocalsAudioPath, destinationVocalsAudioPath);

            songMeta.VocalsAudio = destinationVocalsAudioPath;
            if (destinationFolder == SongMetaUtils.GetDirectoryPath(songMeta))
            {
                songMeta.VocalsAudio = PathUtils.MakeRelativePath(SongMetaUtils.GetDirectoryPath(songMeta), songMeta.VocalsAudio);
            }
            songMetaChanged = true;
        }
        else
        {
            Debug.LogError($"Voice audio not found. Written files: {spleeterResult.WrittenFiles.JoinWith(", ")}");
        }

        // Check instrumental audio
        string instrumentalAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileName(filePath).Contains("accompaniment"));
        if (!instrumentalAudioPath.IsNullOrEmpty()
            && File.Exists(instrumentalAudioPath))
        {
            Debug.Log("Instrumental audio written to: " + instrumentalAudioPath);

            string destinationInstrumentalAudioPath = destinationFolder + "/instrumental.ogg";
            Debug.Log("Moving instrumental audio to: " + destinationInstrumentalAudioPath);
            FileUtils.MoveFileOverwriteIfExists(instrumentalAudioPath, destinationInstrumentalAudioPath);

            songMeta.InstrumentalAudio = destinationInstrumentalAudioPath;
            if (destinationFolder == SongMetaUtils.GetDirectoryPath(songMeta))
            {
                songMeta.InstrumentalAudio = PathUtils.MakeRelativePath(SongMetaUtils.GetDirectoryPath(songMeta), songMeta.InstrumentalAudio);
            }

            songMetaChanged = true;
        }
        else
        {
            Debug.LogError($"Instrumental audio not found. Written files: {spleeterResult.WrittenFiles.JoinWith(", ")}");
        }

        // Remove folder that was created by spleeter
        // string spleeterOutputFolder = Path.GetDirectoryName(instrumentalAudioPath);
        // if (Directory.Exists(spleeterOutputFolder))
        // {
        //     Directory.Delete(spleeterOutputFolder);
        // }

        // Save song meta
        if (songMetaChanged
            && saveSong)
        {
            songMetaManager.SaveSong(songMeta, true);
        }
    }

    private string GetSpleeterCommand(string fallbackAudioSeparationCommand)
    {
        return !settings.SongEditorSettings.AudioSeparationCommand.IsNullOrEmpty()
            ? settings.SongEditorSettings.AudioSeparationCommand
            : fallbackAudioSeparationCommand;
    }

    private Action<string> GetLogAction()
    {
        return message => Debug.Log($"SpleeterRunner: {message}");
    }
}
