using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicPitchRunner;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionManager : MonoBehaviour, INeedInjection
{
    private readonly object lockObject = new();
    private int basicPitchProcessCount;

    public static PitchDetectionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<PitchDetectionManager>();

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly List<Job> pitchDetectionJobs = new();

    private readonly Subject<PitchDetectionFinishedEvent> pitchDetectionFinishedEventStream = new();
    public Subject<PitchDetectionFinishedEvent> PitchDetectionFinishedEventStream => pitchDetectionFinishedEventStream;

    public void ProcessSongMeta(SongMeta songMeta, Job pitchDetectionJob = null)
    {
        ProcessSongMetaAsObservable(songMeta, pitchDetectionJob)
            // Subscribe to trigger observable
            .Subscribe(evt => Debug.Log($"Successfully analyzed pitch: {evt}"));
    }

    public IObservable<BasicPitchDetectionResult> ProcessSongMetaAsObservable(
        SongMeta songMeta,
        Job pitchDetectionJob = null)
    {
        string vocalsAudioUri = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
        if (!FileUtils.Exists(vocalsAudioUri))
        {
            return Observable.Throw<BasicPitchDetectionResult>(
                new Exception($"Vocals audio for '{Path.GetFileName(songMeta.Audio)}' does not exist at path '{vocalsAudioUri}'"));
        }
        if (!ApplicationUtils.IsSupportedBasicPitchDetectionAudioFormat(Path.GetExtension(vocalsAudioUri)))
        {
            return Observable.Throw<BasicPitchDetectionResult>(
                new Exception($"Pitch Detection using Basic Pitch not supported for this audio file.\n" +
                              $"Requires one of {ApplicationUtils.supportedBasicPitchDetectionAudioFiles.JoinWith(", ")}"));
        }

        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);

        // Create job to show in UI
        if (pitchDetectionJob == null)
        {
            pitchDetectionJob = new Job(Translation.Get(R.Messages.job_pitchDetectionWithName,
                "name", Path.GetFileName(songMeta.Audio)));
            jobManager.AddJob(pitchDetectionJob);
        }
        pitchDetectionJob.SetStatus(EJobStatus.Running);

        AudioClip audioClip = AudioManager.LoadAudioClipFromUriImmediately(vocalsAudioUri, true);
        int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
        pitchDetectionJob.EstimatedTotalDurationInMillis = (int)Math.Ceiling(lengthInMillis / 3.0);

        CancellationTokenSource cancellationTokenSource = new();
        pitchDetectionJob.OnCancel = () => cancellationTokenSource.Cancel();
        pitchDetectionJobs.Add(pitchDetectionJob);

        // Set path to Basic Pitch executable if needed
        string fallbackPitchDetectionCommand = PlatformUtils.IsWindows
            ? $"\"{ApplicationUtils.GetStreamingAssetsPath("BasicPitchExe/basic_pitch_exe.exe").Replace("/", "\\")}\" --onset_threshold 0.3 --frame_threshold 0.3"
            : "";

        return DoProcessSongMetaAsObservable(
                songMeta,
                generatedSongFolderAbsolutePath,
                cancellationTokenSource.Token,
                fallbackPitchDetectionCommand)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            // Handle Exceptions
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Pitch detection failed: {ex.Message}");
                pitchDetectionJob.SetResult(EJobResult.Error);
                throw ex;
            })
            .Select(pitchDetectionResult =>
            {
                pitchDetectionJob.SetResult(EJobResult.Ok);

                pitchDetectionFinishedEventStream.OnNext(new PitchDetectionFinishedEvent(songMeta));
                return pitchDetectionResult;
            });
    }

    private IObservable<BasicPitchDetectionResult> DoProcessSongMetaAsObservable(SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        CancellationToken cancellationToken,
        string fallbackCommand)
    {
        if (basicPitchProcessCount > 0)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            return Observable.Throw<BasicPitchDetectionResult>(new IllegalStateException("Already performing pitch detection"));
        }

        return Observable.Create<BasicPitchDetectionResult>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    basicPitchProcessCount++;

                    Debug.Log($"Running basic pitch on vocals audio: {songMeta.VocalsAudio}");
                    UpdateBasicPitchRunnerConfig(fallbackCommand);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(songMeta.Audio);

                    BasicPitchParameters basicPitchParameters = new();
                    basicPitchParameters.InputFile = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio);
                    basicPitchParameters.OutputFolder = $"{generatedSongFolderAbsolutePath}/{fileNameWithoutExtension}";
                    DirectoryUtils.CreateDirectory(basicPitchParameters.OutputFolder);

                    Debug.Log($"Calling BasicPitchRunner with parameters {JsonConverter.ToJson(basicPitchParameters)}");
                    Task<BasicPitchResult> runBasicPitchTask = BasicPitchRunnerUtils.RunBasicPitch(basicPitchParameters, cancellationToken);
                    runBasicPitchTask.Wait();
                    BasicPitchResult basicPitchResult = runBasicPitchTask.Result;

                    if (TryMoveFilesOfBasicPitchResult(songMeta, generatedSongFolderAbsolutePath, basicPitchResult,
                            out string midiFilePath))
                    {
                        o.OnNext(new BasicPitchDetectionResult(midiFilePath));
                    }
                    else
                    {
                        throw new BasicPitchRunnerException("MIDI file output of Basic Pitch not found");
                    }
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                }
                finally
                {
                    basicPitchProcessCount--;
                }

                o.OnCompleted();
            }

            return Disposable.Empty;
        });
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
            Debug.LogError($"Basic Pitch terminated with exit code {basicPitchResult.ExitCode}. Output:\n{basicPitchResult.Output}");
            midiFilePath = "";
            return false;
        }

        if (basicPitchResult.WrittenFiles.IsNullOrEmpty())
        {
            Debug.LogError($"BasicPitchResult.WrittenFiles is empty. Output:\n{basicPitchResult.Output}");
            midiFilePath = "";
            return false;
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

    private void UpdateBasicPitchRunnerConfig(string fallbackCommand)
    {
        Debug.Log($"Updating basic pitch config");
        string basicPitchCommand = !settings.SongEditorSettings.BasicPitchCommand.IsNullOrEmpty()
            ? settings.SongEditorSettings.BasicPitchCommand
            : fallbackCommand;
        BasicPitchRunnerConfig.Create()
            .SetBasicPitchCommand(basicPitchCommand)
            .SetIsWindows(PlatformUtils.IsWindows)
            .SetLogAction(message => Debug.Log($"BasicPitchRunner: {message}"));
    }

    private void OnApplicationQuit()
    {
        pitchDetectionJobs.ForEach(job => job.Cancel());
    }
}
