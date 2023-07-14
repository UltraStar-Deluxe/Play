using System;
using System.Collections;
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

    protected override object GetInstance()
    {
        return Instance;
    }

    public void ConvertInstrumentalAudioToSupportedFormat(SongMeta songMeta)
    {
        Debug.Log($"Convert vocals audio to supported format '{SongMetaUtils.GetInstrumentalAudioUri(songMeta)}'");

        string jobTitle = $"Convert instrumental audio of '{SongMetaUtils.GetArtistDashTitle(songMeta)}'";

        // string ffmpegArguments = "-y -i \"C:/Users/andre/Downloads/TestAudio.flac\" \"C:/Users/andre/Downloads/TestAudio.ogg\"";
        string ffmpegArguments = "-y -i \"C:/Users/andre/Downloads/TestVideo2.mp4\" -c:v libvpx -c:a libvorbis \"C:/Users/andre/Downloads/TestVideo2.webm\"";
        FfmpegCommand ffmpegCommand = CreateFfmpegCommandOnNewGameObject(jobTitle, ffmpegArguments);

        Job uiJob = new(jobTitle);
        uiJob.OnCancel = () => ffmpegCommand.StopFfmpeg();

        jobManager.AddJob(uiJob);

        StartCoroutine(RunFfmpegCommandCoroutine(ffmpegCommand, uiJob));
    }

    private IEnumerator RunFfmpegCommandCoroutine(FfmpegCommand ffmpegCommand, Job uiJob)
    {
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
