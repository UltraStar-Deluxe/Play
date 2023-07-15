using System;
using System.Collections;
using System.IO;
using System.Linq;
using FfmpegUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongMediaFileConversionManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SongMediaFileConversionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongMediaFileConversionManager>();

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    protected override object GetInstance()
    {
        return Instance;
    }

    public void ConvertFileToSupportedFormat(
        SongMeta songMeta,
        string mediaDescription,
        Func<string> pathGetter,
        Action<string> pathSetter,
        bool isAudio)
    {
        string currentValue = pathGetter();
        if (currentValue.IsNullOrEmpty())
        {
            return;
        }

        string sourceFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, currentValue);
        if (!FileUtils.Exists(sourceFilePath))
        {
            string errorMessage = $"File not found '{sourceFilePath}'";
            Debug.Log(errorMessage);
            UiManager.CreateNotification(errorMessage);
            return;
        }

        string targetFileExtension = isAudio ? "ogg" : "webm";
        string sourceFileExtension = PathUtils.GetExtensionWithoutDot(currentValue);
        if (string.Equals(sourceFileExtension, targetFileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            // Nothing to do
            return;
        }

        bool canConvertToSupportedFormat = isAudio
            ? ApplicationUtils.IsFfmpegSupportedAudioFormat(sourceFileExtension)
            : ApplicationUtils.IsFfmpegSupportedVideoFormat(sourceFileExtension);
        if (!canConvertToSupportedFormat)
        {
            string errorMessage = $"Cannot convert {mediaDescription} '{sourceFileExtension}' to supported format";
            Debug.Log(errorMessage);
            UiManager.CreateNotification(errorMessage);
            return;
        }

        Debug.Log($"Converting {mediaDescription} of '{SongMetaUtils.GetArtistDashTitle(songMeta)}' to {targetFileExtension}");
        string jobTitle = $"Convert {mediaDescription} of '{SongMetaUtils.GetArtistDashTitle(songMeta)}' to {targetFileExtension}";

        string targetFilePath = Path.ChangeExtension(sourceFilePath, targetFileExtension);
        string ffmpegArguments = isAudio
            ? $"-y -i \"{sourceFilePath}\" \"{targetFilePath}\""
            : $"-y -i \"{sourceFilePath}\" -c:v libvpx -c:a libvorbis \"{targetFilePath}\"";
        FfmpegCommand ffmpegCommand = CreateFfmpegCommandOnNewGameObject(jobTitle, ffmpegArguments);

        // Create UI job
        Job uiJob = new(jobTitle);
        uiJob.OnCancel = () => ffmpegCommand.StopFfmpeg();
        jobManager.AddJob(uiJob);

        StartCoroutine(RunFfmpegCommandCoroutine(ffmpegCommand, uiJob, () =>
        {
            string relativeTargetFilePath = PathUtils.MakeRelativePath(songMeta.Directory, targetFilePath);
            Debug.Log($"Setting {mediaDescription} of '{SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta)}' to '{relativeTargetFilePath}'");
            songMeta.InstrumentalAudio = relativeTargetFilePath;
            songMetaManager.SaveSong(songMeta, true);
        }));
    }

    public void ConvertVocalsAudioToSupportedFormat(SongMeta songMeta)
    {
        ConvertFileToSupportedFormat(
            songMeta,
            "vocals audio",
            () => songMeta.VocalsAudio,
            newValue => songMeta.VocalsAudio = newValue,
            true);
    }

    public void ConvertInstrumentalAudioToSupportedFormat(SongMeta songMeta)
    {
        ConvertFileToSupportedFormat(
            songMeta,
            "instrumental audio",
            () => songMeta.InstrumentalAudio,
            newValue => songMeta.InstrumentalAudio = newValue,
            true);
    }

    public void ConvertAudioToSupportedFormat(SongMeta songMeta)
    {
        // The MP3 tag can also be used with a video file.
        string fileExtension = PathUtils.GetExtensionWithoutDot(songMeta.Mp3);
        bool isAudio = ApplicationUtils.audioFileExtensions.Contains(fileExtension);

        ConvertFileToSupportedFormat(
            songMeta,
            "audio",
            () => songMeta.Mp3,
            newValue => songMeta.Mp3 = newValue,
            isAudio);
    }

    public void ConvertVideoToSupportedFormat(SongMeta songMeta)
    {
        ConvertFileToSupportedFormat(
            songMeta,
            "video",
            () => songMeta.Video,
            newValue => songMeta.Video = newValue,
            false);
    }

    private IEnumerator RunFfmpegCommandCoroutine(FfmpegCommand ffmpegCommand, Job uiJob, Action onSuccess)
    {
        Debug.Log($"Executing ffmpeg command '{ffmpegCommand.Options}'");

        uiJob.SetStatus(EJobStatus.Running);
        ffmpegCommand.StartFfmpeg();
        yield return new WaitForSeconds(0.1f);

        while (ffmpegCommand.IsRunning
               && !ffmpegCommand.IsFinished)
        {
            if (ffmpegCommand.DurationTime.TotalMilliseconds > uiJob.EstimatedTotalDurationInMillis)
            {
                uiJob.EstimatedTotalDurationInMillis = (long)ffmpegCommand.DurationTime.TotalMilliseconds;
            }

            double newProgressInPercent = Math.Floor(ffmpegCommand.Progress * 100.0);
            if (newProgressInPercent > uiJob.EstimatedCurrentProgressInPercent)
            {
                uiJob.EstimatedCurrentProgressInPercent = newProgressInPercent;
            }
            yield return new WaitForSeconds(0.1f);
        }

        uiJob.SetResult(EJobResult.Ok);

        Debug.Log($"FfmpegCommand finished: {uiJob.Name}");
        Destroy(ffmpegCommand.gameObject);

        if (onSuccess != null)
        {
            try
            {
                onSuccess.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to invoke onSuccess callback after running ffmpeg command '{ffmpegCommand.Options}'");
            }
        }
    }

    private FfmpegCommand CreateFfmpegCommandOnNewGameObject(string gameObjectName, string ffmpegArguments)
    {
        GameObject ffmpegGameObject = new GameObject($"FfmpegCommand '{gameObjectName}'");
        ffmpegGameObject.transform.SetParent(transform);
        FfmpegCommand ffmpegCommand = ffmpegGameObject.AddComponent<FfmpegCommand>();
        ffmpegCommand.Options = ffmpegArguments;
        ffmpegCommand.ExecuteOnStart = false;
        ffmpegCommand.GetProgressOnScript = true;
        // ffmpegCommand.PrintStdErr = settings.LogFfmpegOutput;
        ffmpegCommand.PrintStdErr = true;
        return ffmpegCommand;
    }
}
