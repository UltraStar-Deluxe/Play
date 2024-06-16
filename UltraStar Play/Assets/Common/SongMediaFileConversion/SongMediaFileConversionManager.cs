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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        MinTargetFileSizeInBytes = DefaultMinTargetFileSizeInBytes;
    }

    private const int DefaultMinTargetFileSizeInBytes = 100 * 1024; // 100 KB
    private const int MaxConversionRetry = 3;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly List<IEnumerator> runningSongMediaConversionCoroutines = new();
    private readonly List<IEnumerator> pendingSongMediaConversionCoroutines = new();

    public static int MinTargetFileSizeInBytes { get; set; } = DefaultMinTargetFileSizeInBytes;

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

    private void ConvertSongMetaMediaFileToSupportedFormat(
        SongMeta songMeta,
        string mediaDescription,
        Func<string> pathGetter,
        Action<string> pathSetter,
        Translation jobTitle,
        bool isAudio)
    {
        ConvertSongMetaMediaFileToSupportedFormatWithRetry(
            songMeta,
            mediaDescription,
            pathGetter,
            pathSetter,
            jobTitle,
            isAudio);
    }

    private void ConvertSongMetaMediaFileToSupportedFormatWithRetry(
        SongMeta songMeta,
        string mediaDescription,
        Func<string> pathGetter,
        Action<string> pathSetter,
        Translation jobTitle,
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
            Debug.LogError($"Cannot convert file '{sourceFilePath}', file does not exist");
            return;
        }

        void OnSuccess(string targetFilePath)
        {
            string relativeTargetFilePath = PathUtils.MakeRelativePath(SongMetaUtils.GetDirectoryPath(songMeta), targetFilePath);
            Debug.Log($"Setting {mediaDescription} of '{SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta)}' to '{relativeTargetFilePath}'");
            pathSetter(relativeTargetFilePath);
            songMetaManager.SaveSong(songMeta, true);
            songMetaManager.ReloadSong(songMeta);
            Debug.Log($"Successfully converted {mediaDescription} of '{SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta)}' to '{relativeTargetFilePath}'");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_convertMediaSuccess,
                "name", songMeta.GetArtistDashTitle()));
        }

        void OnFailure(ConversionError conversionError)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_failedToConvert,
                "name", songMeta.GetArtistDashTitle()));
        }

        ConvertFileToSupportedFormat(sourceFilePath,
            mediaDescription,
             jobTitle,
            isAudio,
            true,
            MaxConversionRetry,
            OnSuccess,
            OnFailure);
    }

    private static ConversionError GetConversionError(string sourceFilePath, string targetFilePath)
    {
        if (!FileUtils.Exists(targetFilePath))
        {
            return new ConversionError(
                sourceFilePath,
                targetFilePath,
                $"Failed to convert file '{sourceFilePath}'. Target file not found: {targetFilePath}",
                false);
        }

        long targetFileSizeInBytes = new FileInfo(targetFilePath).Length;
        if (targetFileSizeInBytes < MinTargetFileSizeInBytes)
        {
            return new ConversionError(
                sourceFilePath,
                targetFilePath,
                $"Failed to convert file '{sourceFilePath}'. Target file is too small: '{targetFilePath}', size in bytes: {targetFileSizeInBytes}",
                true);
        }

        return null;
    }

    public void ConvertFileToSupportedFormat(
        string sourceFilePath,
        string mediaDescription,
        Translation jobTitle,
        bool isAudio,
        bool ignoreEqualFileExtension,
        int maxRetry,
        Action<string> onSuccess,
        Action<ConversionError> onFailure)
    {
        if (!FileUtils.Exists(sourceFilePath))
        {
            Debug.Log($"File not found '{sourceFilePath}'");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_fileNotFoundWithName,
                "name", sourceFilePath));
            return;
        }

        string sourceFileExtension = PathUtils.GetExtensionWithoutDot(sourceFilePath);
        string sourceFilePathWithoutExtension = $"{Path.GetDirectoryName(sourceFilePath)}/{Path.GetFileNameWithoutExtension(sourceFilePath)}";
        if (!settings.FileFormatToFfmpegConversionArguments.TryGetValue(sourceFileExtension, out string ffmpegArgumentsWithPlaceholders))
        {
            if ((isAudio && !settings.FileFormatToFfmpegConversionArguments.TryGetValue("ANY_AUDIO", out ffmpegArgumentsWithPlaceholders))
                || (!isAudio && !settings.FileFormatToFfmpegConversionArguments.TryGetValue("ANY_VIDEO", out ffmpegArgumentsWithPlaceholders)))
            {
                ffmpegArgumentsWithPlaceholders = isAudio
                    ? $"-y -i \"INPUT_FILE\" \"INPUT_FILE_WITHOUT_EXTENSION.ogg\""
                    : $"-y -i \"INPUT_FILE\" -c:v libvpx -c:a libvorbis \"INPUT_FILE_WITHOUT_EXTENSION.webm\"";
            }
        }

        string ffmpegArguments = ffmpegArgumentsWithPlaceholders
            // Replace longer placeholders first
            .Replace("INPUT_FILE_WITHOUT_EXTENSION", $"{sourceFilePathWithoutExtension}")
            .Replace("INPUT_FILE", sourceFilePath);

        string targetFileName = GetTargetFileNameFromFfmpegArguments(ffmpegArguments);
        if (targetFileName.IsNullOrEmpty())
        {
            Debug.Log($"Unable to determine target file name for '{sourceFilePath}'");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
            return;
        }

        string targetFileExtension = PathUtils.GetExtensionWithoutDot(targetFileName);
        if (targetFileExtension.IsNullOrEmpty())
        {
            Debug.Log($"Unable to determine target file extension for '{sourceFilePath}'");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
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
            Debug.Log($"Cannot convert {mediaDescription} '{sourceFileExtension}' to supported format");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
            return;
        }

        string targetFolder = Path.GetDirectoryName(sourceFilePath);
        string targetFilePath = $"{targetFolder}/{targetFileName}";

        Debug.Log($"Converting {mediaDescription} of '{sourceFilePath}' to '{targetFilePath}'");
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

            ConversionError conversionError = GetConversionError(sourceFilePath, targetFilePath);
            if (conversionError == null)
            {
                onSuccess?.Invoke(targetFilePath);
            }
            else
            {
                if (conversionError.CanRetry
                    && maxRetry > 0)
                {
                    Debug.LogError($"{conversionError.ErrorMessage}. Remaining retry count: {maxRetry}. Attempting retry.");
                    ConvertFileToSupportedFormat(
                        sourceFilePath,
                        mediaDescription,
                        jobTitle,
                        isAudio,
                        ignoreEqualFileExtension,
                        maxRetry - 1,
                        onSuccess,
                        onFailure);
                    return;
                }
                else if (conversionError.CanRetry
                         && maxRetry <= 0)
                {
                    Debug.LogError($"{conversionError.ErrorMessage}. Failed last retry.");
                    onFailure?.Invoke(conversionError);
                }
                else
                {
                    Debug.LogError($"{conversionError.ErrorMessage}. Cannot retry.");
                    onFailure?.Invoke(conversionError);
                }
            }
        });

        pendingSongMediaConversionCoroutines.Add(runFfmpegCommandCoroutine);
    }

    public static string GetTargetFileNameFromFfmpegArguments(string ffmpegArguments)
    {
        // Return the last found file name
        MatchCollection matches = Regex.Matches(ffmpegArguments, @"(/|\\)(?<fileNameWithoutExtension>[^/\\]+)\.(?<extension>\w+)");
        if (matches.Count == 0)
        {
            return "";
        }

        Match lastMatch = matches.LastOrDefault();
        string fileExtension = lastMatch.Groups["extension"].Value.ToLowerInvariant();
        string fileNameWithoutExtension = lastMatch.Groups["fileNameWithoutExtension"].Value;
        string fileName = $"{fileNameWithoutExtension}.{fileExtension}";
        return fileName;
    }

    public void ConvertVocalsAudioToSupportedFormat(SongMeta songMeta)
    {
        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "vocals audio",
            () => songMeta.VocalsAudio,
            newValue => songMeta.VocalsAudio = newValue,
            Translation.Get(R.Messages.job_convertVocalsAudioWithName,
                "name", songMeta.GetArtistDashTitle()),
            true);
    }

    public void ConvertInstrumentalAudioToSupportedFormat(SongMeta songMeta)
    {
        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "instrumental audio",
            () => songMeta.InstrumentalAudio,
            newValue => songMeta.InstrumentalAudio = newValue,
            Translation.Get(R.Messages.job_convertInstrumentalAudioWithName,
                "name", songMeta.GetArtistDashTitle()),
            true);
    }

    public void ConvertAudioToSupportedFormat(SongMeta songMeta)
    {
        // The MP3 tag can also be used with a video file.
        string fileExtension = PathUtils.GetExtensionWithoutDot(songMeta.Audio);
        bool isAudio = ApplicationUtils.audioFileExtensions.Contains(fileExtension);

        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "audio",
            () => songMeta.Audio,
            newValue => songMeta.Audio = newValue,
            Translation.Get(R.Messages.job_convertAudioWithName,
                            "name", songMeta.GetArtistDashTitle()),
            isAudio);
    }

    public void ConvertVideoToSupportedFormat(SongMeta songMeta)
    {
        ConvertSongMetaMediaFileToSupportedFormat(
            songMeta,
            "video",
            () => songMeta.Video,
            newValue => songMeta.Video = newValue,
            Translation.Get(R.Messages.job_convertVideoWithName,
                "name", songMeta.GetArtistDashTitle()),
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

    public class ConversionError
    {
        public string SourceFilePath { get; private set; }
        public string TargetFilePath { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool CanRetry { get; private set; }

        public ConversionError(
            string sourceFilePath,
            string targetFilePath,
            string errorMessage,
            bool canRetry)
        {
            SourceFilePath = sourceFilePath;
            TargetFilePath = targetFilePath;
            ErrorMessage = errorMessage;
            CanRetry = canRetry;
        }
    }
}
