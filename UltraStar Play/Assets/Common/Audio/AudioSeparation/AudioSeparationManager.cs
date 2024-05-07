using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpleeterSharp;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AudioSeparationManager : MonoBehaviour, INeedInjection
{
    public static AudioSeparationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<AudioSeparationManager>();

    private readonly object lockObject = new();
    private int audioSeparationProcessCount;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly Subject<AudioSeparationFinishedEvent> audioSeparationFinishedEventStream = new();
    public Subject<AudioSeparationFinishedEvent> AudioSeparationFinishedEventStream => audioSeparationFinishedEventStream;

    public void ProcessSongMeta(
        SongMeta songMeta,
        bool saveSong,
        Job audioSeparationJob = null)
    {
        ProcessSongMetaAsObservable(songMeta, saveSong, audioSeparationJob)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Vocals isolation failed: {ex.Message}");
                NotificationManager.CreateNotification(Translation.Get(Translation.Get(R.Messages.job_audioSeparation_errorWithReason,
                    "reason", ex.Message)));
            })
            // Subscribe to trigger the observable
            .Subscribe(evt => Debug.Log($"Successfully separated audio: {evt}"));
    }

    public IObservable<AudioSeparationResult> ProcessSongMetaAsObservable(
        SongMeta songMeta,
        bool saveSong,
        Job audioSeparationJob = null)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);

        // Create job to show in UI
        if (audioSeparationJob == null)
        {
            audioSeparationJob = new Job(Translation.Get(R.Messages.job_audioSeparationWithName,
                "name", Path.GetFileName(songMeta.Audio)));
            jobManager.AddJob(audioSeparationJob);
        }
        audioSeparationJob.SetStatus(EJobStatus.Running);

        if (ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(audioUri)))
        {
            AudioClip audioClip = AudioManager.LoadAudioClipFromUriImmediately(audioUri, false);
            int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
            audioSeparationJob.EstimatedTotalDurationInMillis = (int)Math.Ceiling(lengthInMillis / 2.0);
        }

        CancellationTokenSource cancellationTokenSource = new();
        audioSeparationJob.OnCancel = () => cancellationTokenSource.Cancel();

        // Set path to Spleeter executable if needed
        string fallbackAudioSeparationCommand = PlatformUtils.IsWindows
            ? $"\"{ApplicationUtils.GetStreamingAssetsPath("SpleeterMsvcExe/Spleeter.exe").Replace("/", "\\")}\""
            : "";

        return Observable.Create<bool>(o =>
                {
                    string fileExtension = Path.GetExtension(new Uri(audioUri).LocalPath);
                    if (!ApplicationUtils.IsSupportedVocalsSeparationAudioFormat(fileExtension))
                    {
                        o.OnError(new Exception(
                            $"Vocals isolation not supported for this audio file.\n" +
                            $"Requires one of {ApplicationUtils.supportedVocalsSeparationAudioFiles.JoinWith(", ")}"));
                    }
                    o.OnNext(true);
                    o.OnCompleted();
                    return Disposable.Empty;
                })
            .ContinueWith(DoProcessSongMetaAsObservable(
                        songMeta,
                        generatedSongFolderAbsolutePath,
                        cancellationTokenSource.Token,
                        fallbackAudioSeparationCommand,
                        saveSong))
            .SubscribeOn(Scheduler.ThreadPool)
            .ObserveOnMainThread()
            .Select(audioSeparationResult =>
            {
                audioSeparationJob.SetResult(EJobResult.Ok);

                audioSeparationFinishedEventStream.OnNext(new AudioSeparationFinishedEvent(songMeta));
                return audioSeparationResult;
            })
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Vocals isolation failed: {ex.Message}");
                audioSeparationJob.SetResult(EJobResult.Error);
                throw ex;
            });
    }

    private IObservable<AudioSeparationResult> DoProcessSongMetaAsObservable(SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        CancellationToken cancellationToken,
        string fallbackAudioSeparationCommand,
        bool saveSong)
    {
        if (audioSeparationProcessCount > 0)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            return Observable.Throw<AudioSeparationResult>(new IllegalStateException("Already performing vocals isolation"));
        }

        return Observable.Create<AudioSeparationResult>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    audioSeparationProcessCount++;

                    Debug.Log($"Separating voice and instrumental audio from song: {songMeta}");
                    UpdateSpleeterSharpConfig(fallbackAudioSeparationCommand);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(songMeta.Audio);

                    SpleeterParameters spleeterParameters = new();
                    spleeterParameters.InputFile = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio);
                    spleeterParameters.OutputFolder = $"{generatedSongFolderAbsolutePath}/{fileNameWithoutExtension}.ogg";
                    spleeterParameters.Overwrite = true;

                    Debug.Log($"Calling SpleeterSharp with parameters {JsonConverter.ToJson(spleeterParameters)}");
                    Task<SpleeterResult> splitTask = SpleeterUtils.SplitAsync(spleeterParameters, cancellationToken);
                    splitTask.Wait();
                    SpleeterResult spleeterResult = splitTask.Result;

                    UpdateSongMetaWithSpleeterResult(songMeta, generatedSongFolderAbsolutePath, spleeterResult, saveSong);

                    string originalAudioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio);
                    string vocalsAudioFilePath = songMeta.VocalsAudio;
                    string instrumentalAudioFilePath = songMeta.InstrumentalAudio;
                    o.OnNext(new AudioSeparationResult(originalAudioFilePath, vocalsAudioFilePath, instrumentalAudioFilePath));

                    // long startTime = TimeUtils.GetUnixTimeMilliseconds();
                    // while (TimeUtils.GetUnixTimeMilliseconds() - startTime < 10000)
                    // {
                    //     if (cancellationToken.IsCancellationRequested)
                    //     {
                    //         Debug.Log("Cancel requested");
                    //         cancellationToken.ThrowIfCancellationRequested();
                    //         break;
                    //     }
                    //     Thread.Sleep(200);
                    // }
                    // o.OnNext(new AudioSeparationResult("", "", ""));
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                }
                finally
                {
                    audioSeparationProcessCount--;
                }

                o.OnCompleted();
            }

            return Disposable.Empty;
        });
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

    private void UpdateSpleeterSharpConfig(string fallbackAudioSeparationCommand)
    {
        Debug.Log($"Updating spleeter config");
        string audioSeparationCommand = !settings.SongEditorSettings.AudioSeparationCommand.IsNullOrEmpty()
            ? settings.SongEditorSettings.AudioSeparationCommand
            : fallbackAudioSeparationCommand;
        SpleeterSharpConfig.Create()
            .SetSpleeterCommand(audioSeparationCommand)
            .SetIsWindows(PlatformUtils.IsWindows)
            .SetLogAction(message => Debug.Log($"SpleeterSharp: {message}"));
    }
}
