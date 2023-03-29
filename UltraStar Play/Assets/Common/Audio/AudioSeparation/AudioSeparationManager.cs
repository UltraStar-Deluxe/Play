using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpleeterSharp;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AudioSeparationManager : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        lockObject = new();
        audioSeparationProcessCount = 0;
    }
    private static object lockObject = new();
    private static int audioSeparationProcessCount;

    public static AudioSeparationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<AudioSeparationManager>();

    [Inject]
    private AudioManager audioManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly List<Job> audioSeparationJobs = new();

    private readonly Subject<AudioSeparationFinishedEvent> audioSeparationFinishedEventStream = new();
    public Subject<AudioSeparationFinishedEvent> AudioSeparationFinishedEventStream => audioSeparationFinishedEventStream;

    public IObservable<AudioSeparationResult> ProcessSongMeta(SongMeta songMeta, Job audioSeparationJob = null)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        string fileExtension = Path.GetExtension(new Uri(audioUri).LocalPath);
        if (!ApplicationUtils.IsSupportedVocalsSeparationAudioFormat(fileExtension))
        {
            UiManager.CreateNotification($"Vocals separation not supported for {fileExtension} files.\n" +
                                         $"Requires one of {ApplicationUtils.supportedVocalsSeparationAudioFiles.ToCsv()}");
            return Observable.Empty<AudioSeparationResult>();
        }
        
        string generatedSongFolderAbsolutePath = ApplicationUtils.GetGeneratedSongFolderAbsolutePath();

        // Create job to show in UI
        if (audioSeparationJob == null)
        {
            audioSeparationJob = new Job($"Vocals separation of '{Path.GetFileName(songMeta.Mp3)}'");
            jobManager.AddJob(audioSeparationJob);
        }
        audioSeparationJob.SetStatus(EJobStatus.Running);

        AudioClip audioClip = audioManager.LoadAudioClipFromUri(audioUri, false);
        int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
        audioSeparationJob.EstimatedTotalDurationInMillis = (int)Math.Ceiling(lengthInMillis / 2.0);

        CancellationTokenSource cancellationTokenSource = new();
        audioSeparationJob.OnCancel = () => cancellationTokenSource.Cancel();
        audioSeparationJobs.Add(audioSeparationJob);

        Subject<AudioSeparationResult> processSongSubject = new();
        DoProcessSongMetaAsObservable(
                songMeta,
                generatedSongFolderAbsolutePath,
                cancellationTokenSource.Token)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                audioSeparationJob.SetResult(EJobResult.Error);
                processSongSubject.OnError(ex);
            })
            .Subscribe(audioSeparationResult =>
            {
                audioSeparationJob.SetResult(EJobResult.Ok);
                processSongSubject.OnNext(audioSeparationResult);
                processSongSubject.OnCompleted();

                audioSeparationFinishedEventStream.OnNext(new AudioSeparationFinishedEvent(songMeta));
            });

        return processSongSubject;
    }

    private IObservable<AudioSeparationResult> DoProcessSongMetaAsObservable(
        SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        CancellationToken cancellationToken)
    {
        if (audioSeparationProcessCount > 0)
        {
            UiManager.CreateNotification("Already performing vocals separation");
            return Observable.Throw<AudioSeparationResult>(new IllegalStateException("Already performing vocals separation"));
        }

        return Observable.Create<AudioSeparationResult>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    audioSeparationProcessCount++;

                    Debug.Log($"Separating voice and instrumental audio from song: {songMeta}");
                    UpdateSpleeterSharpConfig();

                    SpleeterParameters spleeterParameters = new();
                    spleeterParameters.InputFile = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3);
                    spleeterParameters.OutputFolder = generatedSongFolderAbsolutePath;
                    spleeterParameters.OutputFileCodec = "ogg";

                    Debug.Log($"Calling SpleeterSharp with parameters {JsonConverter.ToJson(spleeterParameters)}");
                    Task<SpleeterResult> splitTask = SpleeterUtils.SplitAsync(spleeterParameters, cancellationToken);
                    splitTask.Wait();
                    SpleeterResult spleeterResult = splitTask.Result;

                    UpdateSongMetaWithSpleeterResult(songMeta, generatedSongFolderAbsolutePath, spleeterResult);

                    string originalAudioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3);
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

    private void UpdateSongMetaWithSpleeterResult(SongMeta songMeta, string generatedSongFolderAbsolutePath, SpleeterResult spleeterResult)
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
        string destinationFolder = DirectoryUtils.IsSubDirectory(songMeta.Directory, generatedSongFolderAbsolutePath)
            ? songMeta.Directory
            : ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, songMeta.Directory);
        if (!destinationFolder.IsNullOrEmpty()
            && !Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        // Check voice audio
        string vocalsAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileNameWithoutExtension(filePath) == "vocals");
        if (!vocalsAudioPath.IsNullOrEmpty()
            && File.Exists(vocalsAudioPath))
        {
            Debug.Log("Voice audio written to: " + vocalsAudioPath);

            string destinationVocalsAudioPath = destinationFolder + $"/vocals.ogg";
            Debug.Log("Moving voice audio to: " + destinationVocalsAudioPath);
            FileUtils.MoveFileOverwriteIfExists(vocalsAudioPath, destinationVocalsAudioPath);

            songMeta.VocalsAudio = destinationVocalsAudioPath;
            songMetaChanged = true;
        }
        else
        {
            Debug.LogError($"Voice audio not found. Written files: {spleeterResult.WrittenFiles.ToCsv()}");
        }

        // Check instrumental audio
        string instrumentalAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileNameWithoutExtension(filePath) == "accompaniment");
        if (!instrumentalAudioPath.IsNullOrEmpty()
            && File.Exists(instrumentalAudioPath))
        {
            Debug.Log("Instrumental audio written to: " + instrumentalAudioPath);

            string destinationInstrumentalAudioPath = destinationFolder + "/instrumental.ogg";
            Debug.Log("Moving instrumental audio to: " + destinationInstrumentalAudioPath);
            FileUtils.MoveFileOverwriteIfExists(instrumentalAudioPath, destinationInstrumentalAudioPath);

            songMeta.InstrumentalAudio = destinationInstrumentalAudioPath;
            songMetaChanged = true;
        }
        else
        {
            Debug.LogError($"Instrumental audio not found. Written files: {spleeterResult.WrittenFiles.ToCsv()}");
        }

        // Remove folder that was created by spleeter
        string spleeterOutputFolder = Path.GetDirectoryName(instrumentalAudioPath);
        if (Directory.Exists(spleeterOutputFolder))
        {
            Directory.Delete(spleeterOutputFolder);
        }

        // Save song meta
        if (songMetaChanged)
        {
            songMetaManager.SaveSong(songMeta, true);
        }
    }

    private void UpdateSpleeterSharpConfig()
    {
        Debug.Log($"Updating spleeter config");
        SpleeterSharpConfig.Create()
            .SetSpleeterCommand(settings.SongEditorSettings.AudioSeparationCommand)
            .SetIsWindows(PlatformUtils.IsWindows())
            .SetLogAction(message => Debug.Log($"SpleeterSharp: {message}"));
    }

    private void OnApplicationQuit()
    {
        audioSeparationJobs.ForEach(job => job.Cancel());
    }
}
