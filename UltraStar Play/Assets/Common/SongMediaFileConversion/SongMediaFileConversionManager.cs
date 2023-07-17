using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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

    private readonly List<IEnumerator> runningSongMediaConversionCoroutines = new();
    private readonly List<IEnumerator> pendingSongMediaConversionCoroutines = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    private void Update()
    {
        if (pendingSongMediaConversionCoroutines.Count > 0
            && (runningSongMediaConversionCoroutines.Count < settings.MaxConcurrentSongMediaConversions
                || settings.MaxConcurrentSongMediaConversions <= 0))
        {
            IEnumerator coroutine = pendingSongMediaConversionCoroutines[0];
            pendingSongMediaConversionCoroutines.RemoveAt(0);
            runningSongMediaConversionCoroutines.Add(coroutine);
            Debug.Log($"Started conversion coroutine. Now running conversion coroutines: {runningSongMediaConversionCoroutines.Count}");

            StartCoroutine(CoroutineUtils.Sequence(coroutine,
                CoroutineUtils.ExecuteAction(() =>
                {
                    runningSongMediaConversionCoroutines.Remove(coroutine);
                    Debug.Log($"Finished conversion coroutine. Now running conversion coroutines: {runningSongMediaConversionCoroutines.Count}");
                })));
        }
    }

    protected void ConvertSongMetaMediaFileToSupportedFormat(
        SongMeta songMeta,
        string mediaDescription,
        Func<string> pathGetter,
        Action<string> pathSetter,
        string jobTitle,
        bool isAudio)
    {
        string currentValue = pathGetter();
        if (currentValue.IsNullOrEmpty())
        {
            return;
        }

        void OnSuccess(string targetFilePath)
        {
            string relativeTargetFilePath = PathUtils.MakeRelativePath(songMeta.Directory, targetFilePath);
            Debug.Log($"Setting {mediaDescription} of '{SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta)}' to '{relativeTargetFilePath}'");
            pathSetter(relativeTargetFilePath);
            songMetaManager.SaveSong(songMeta, true);
        }

        string sourceFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, currentValue);

        ConvertFileToSupportedFormat(sourceFilePath, mediaDescription, jobTitle, isAudio, true, OnSuccess);
    }

    public void ConvertFileToSupportedFormat(
        string sourceFilePath,
        string mediaDescription,
        string jobTitle,
        bool isAudio,
        bool ignoreEqualFileExtension,
        Action<string> onSuccess)
    {
        if (!FileUtils.Exists(sourceFilePath))
        {
            string errorMessage = $"File not found '{sourceFilePath}'";
            Debug.Log(errorMessage);
            UiManager.CreateNotification(errorMessage);
            return;
        }

        string sourceFileExtension = PathUtils.GetExtensionWithoutDot(sourceFilePath);
        if (!settings.FileFormatToFfmpegConversionArguments.TryGetValue(sourceFileExtension, out string ffmpegArguments))
        {
            if ((isAudio && !settings.FileFormatToFfmpegConversionArguments.TryGetValue("ANY_AUDIO", out ffmpegArguments))
                || (!isAudio && !settings.FileFormatToFfmpegConversionArguments.TryGetValue("ANY_VIDEO", out ffmpegArguments)))
            {
                ffmpegArguments = isAudio
                    ? $"-y -i \"INPUT_FILE\" \"INPUT_FILE_WITHOUT_EXTENSION.ogg\""
                    : $"-y -i \"INPUT_FILE\" -c:v libvpx -c:a libvorbis \"INPUT_FILE_WITHOUT_EXTENSION.webm\"";
            }
        }

        string targetFileExtension = GetTargetFileExtensionFromFfmpegArgumentsTemplate(ffmpegArguments);
        if (targetFileExtension.IsNullOrEmpty())
        {
            string errorMessage = $"Unable to determine target file extension for '{sourceFilePath}'";
            Debug.Log(errorMessage);
            UiManager.CreateNotification(errorMessage);
            return;
        }

        if (string.Equals(sourceFileExtension, targetFileExtension, StringComparison.InvariantCultureIgnoreCase)
            && !ignoreEqualFileExtension)
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

        Debug.Log($"Converting {mediaDescription} of '{sourceFilePath}' to {targetFileExtension}");

        string sourceFilePathWithoutExtension = $"{Path.GetDirectoryName(sourceFilePath)}/{Path.GetFileNameWithoutExtension(sourceFilePath)}";
        string targetFilePathWithoutExtension = sourceFilePathWithoutExtension;
        string targetFilePath = $"{targetFilePathWithoutExtension}.{targetFileExtension}";
        ffmpegArguments = ffmpegArguments
            // Replace longer placeholders first
            .Replace("INPUT_FILE_WITHOUT_EXTENSION", $"{sourceFilePathWithoutExtension}")
            .Replace("INPUT_FILE", sourceFilePath);

        FfmpegCommand ffmpegCommand = CreateFfmpegCommandOnNewGameObject(jobTitle, ffmpegArguments);

        // Create UI job
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Job uiJob = new(jobTitle);
        uiJob.OnCancel = () =>
        {
            uiJob.SetResult(EJobResult.Error);
            cancellationTokenSource.Cancel();
            ffmpegCommand.StopFfmpeg();
        };
        jobManager.AddJob(uiJob);

        IEnumerator runFfmpegCommandCoroutine = RunFfmpegCommandCoroutine(ffmpegCommand, uiJob, cancellationTokenSource.Token, () =>
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            onSuccess?.Invoke(targetFilePath);
        });

        pendingSongMediaConversionCoroutines.Add(runFfmpegCommandCoroutine);
    }

    private string GetTargetFileExtensionFromFfmpegArgumentsTemplate(string ffmpegArguments)
    {
        // Return the last found file extension
        MatchCollection matches = Regex.Matches(ffmpegArguments, @"\.(?<extension>\w+)");
        if (matches.Count == 0)
        {
            return "";
        }

        Match lastMatch = matches.LastOrDefault();
        return lastMatch.Groups["extension"].Value.ToLowerInvariant();
    }

    public void ConvertVocalsAudioToSupportedFormat(SongMeta songMeta)
    {
        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "vocals audio",
            () => songMeta.VocalsAudio,
            newValue => songMeta.VocalsAudio = newValue,
            $"Convert vocals audio of {SongMetaUtils.GetArtistDashTitle(songMeta)} to supported format",
            true);
    }

    public void ConvertInstrumentalAudioToSupportedFormat(SongMeta songMeta)
    {
        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "instrumental audio",
            () => songMeta.InstrumentalAudio,
            newValue => songMeta.InstrumentalAudio = newValue,
            $"Convert instrumental audio of {SongMetaUtils.GetArtistDashTitle(songMeta)} to supported format",
            true);
    }

    public void ConvertAudioToSupportedFormat(SongMeta songMeta)
    {
        // The MP3 tag can also be used with a video file.
        string fileExtension = PathUtils.GetExtensionWithoutDot(songMeta.Mp3);
        bool isAudio = ApplicationUtils.audioFileExtensions.Contains(fileExtension);

        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "audio",
            () => songMeta.Mp3,
            newValue => songMeta.Mp3 = newValue,
            $"Convert audio of {SongMetaUtils.GetArtistDashTitle(songMeta)} to supported format",
            isAudio);
    }

    public void ConvertVideoToSupportedFormat(SongMeta songMeta)
    {
        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "video",
            () => songMeta.Video,
            newValue => songMeta.Video = newValue,
            $"Convert video audio of {SongMetaUtils.GetArtistDashTitle(songMeta)} to supported format",
            false);
    }

    private IEnumerator RunFfmpegCommandCoroutine(FfmpegCommand ffmpegCommand, Job uiJob, CancellationToken cancellationToken, Action onSuccess)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Debug.Log($"Not executing ffmpeg command '{ffmpegCommand.Options}'. Cancelled.");
            yield break;
        }

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

        if (cancellationToken.IsCancellationRequested)
        {
            Debug.Log($"FfmpegCommand not finished: '{uiJob.Name}'. Cancelled.");
            yield break;
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
        ffmpegCommand.PrintStdErr = settings.LogFfmpegOutput;
        return ffmpegCommand;
    }
}
