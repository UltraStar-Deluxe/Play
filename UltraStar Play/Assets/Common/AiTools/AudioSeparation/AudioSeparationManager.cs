using System;
using System.IO;
using System.Threading;
using Eitan.Sherpa.Onnx.Unity.Mono.Components;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AudioSeparationManager : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    private const string InstrumentalStemName = "non-vocals";
    private const string VocalsStemName = "vocals";
    
    public static AudioSeparationManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<AudioSeparationManager>();

    private readonly SemaphoreSlim audioSeparationProcessSemaphore = new(1, 1);

    [InjectedInInspector]
    public SourceSeparationComponent sourceSeparationComponent;
    
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

    private bool isSourceSeparationModuleReady;
    
    public void OnInjectionFinished()
    {
        sourceSeparationComponent.SeparationReadyEvent.AddListener(OnSeparationReady);
        sourceSeparationComponent.ErrorEvent.AddListener(OnError);
        sourceSeparationComponent.InitializationStateChangedEvent.AddListener(OnInitializationStateChangedEvent);
    }
    
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

        AudioClip audioClip = await AudioSampleLoader.Instance.LoadAsAudioClip(audioUri);

        // Estimate duration
        int lengthInMillis = (int)Math.Floor(audioClip.length * 1000);
        jobProgress.EstimatedTotalDurationInMillis = (int)Math.Ceiling((double)lengthInMillis);

        string fileExtension = Path.GetExtension(new Uri(audioUri).LocalPath);
        if (!ApplicationUtils.IsSupportedVocalsSeparationAudioFormat(fileExtension))
        {
            throw new AudioSeparationException($"Vocals isolation not supported for this audio file. Requires one of {ApplicationUtils.supportedVocalsSeparationAudioFiles.JoinWith(", ")}");
        }

        AudioSeparationResult audioSeparationResult = await ProcessSongMetaWithAiAsync(
            songMeta,
            generatedSongFolderAbsolutePath,
            jobProgress.CancellationTokenSource.Token,
            saveSong,
            audioClip);

        audioSeparationFinishedEventStream.OnNext(new AudioSeparationFinishedEvent(songMeta));
        return audioSeparationResult;
    }

    private async Awaitable<AudioSeparationResult> ProcessSongMetaWithAiAsync(
        SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        CancellationToken cancellationToken,
        bool saveSong,
        AudioClip audioClip)
    {
        // Instant fail if already locked (timeout 0)
        if (!await audioSeparationProcessSemaphore.WaitAsync(0, cancellationToken))
        {
            throw new JobAlreadyRunningException(new AudioSeparationException("Already performing vocals isolation"));
        }

        // Make sure source separation module has been loaded.
        if (!isSourceSeparationModuleReady)
        {
            sourceSeparationComponent.TryLoadModule();
            await ConditionUtils.WaitForConditionAsync(() => isSourceSeparationModuleReady,
                new WaitForConditionConfig { timeoutInMillis = 30_000 });
        }

        try
        {
            Debug.Log($"Separating voice and instrumental audio from song: {songMeta}");
            string originalAudioFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio);

            SourceSeparationComponent.SeparatedClipSet separatedClipSet = await sourceSeparationComponent.SeparateClipAsync(audioClip, false, cancellationToken);
            if (separatedClipSet == null)
            {
                throw new AudioSeparationException("separatedClipSet is null");
            }

            SaveAudioFilesAndUpdateSongMeta(songMeta, generatedSongFolderAbsolutePath, saveSong, separatedClipSet);

            string vocalsAudioFilePath = songMeta.VocalsAudio;
            string instrumentalAudioFilePath = songMeta.InstrumentalAudio;
            return new AudioSeparationResult(originalAudioFilePath, vocalsAudioFilePath, instrumentalAudioFilePath);
        }
        finally
        {
            audioSeparationProcessSemaphore.Release();
        }
    }

    private void SaveAudioFilesAndUpdateSongMeta(
        SongMeta songMeta,
        string generatedSongFolderAbsolutePath,
        bool saveSong,
        SourceSeparationComponent.SeparatedClipSet separatedClipSet)
    {
        // Prepare directory to save audio files.
        // Prepare directory to move created audio files.
        string destinationFolder = DirectoryUtils.IsSubDirectory(SongMetaUtils.GetDirectoryPath(songMeta), generatedSongFolderAbsolutePath)
            ? SongMetaUtils.GetDirectoryPath(songMeta)
            : ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, SongMetaUtils.GetDirectoryPath(songMeta));
        
        if (!destinationFolder.IsNullOrEmpty()
            && !Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        // Save audio files
        bool songMetaChanged = false;
        foreach (SourceSeparationComponent.SeparatedStemClip stem in separatedClipSet.stems)
        {
            string fileBaseName = stem.stemName.Replace(InstrumentalStemName, "instrumental");
            string outputPath = $"{destinationFolder}/{fileBaseName}.ogg";
            Debug.Log($"Saving separated audio file. stem: '{stem.stemName}', outputPath: '{outputPath}'");
            OggFileWriter.WriteFile(outputPath, stem.clip);

            string relativeOrAbsoluteOutputPath = outputPath;
            if (destinationFolder == SongMetaUtils.GetDirectoryPath(songMeta))
            {
                relativeOrAbsoluteOutputPath = PathUtils.MakeRelativePath(SongMetaUtils.GetDirectoryPath(songMeta), outputPath);
            }

            if (stem.stemName == InstrumentalStemName)
            {
                songMeta.InstrumentalAudio = relativeOrAbsoluteOutputPath;
                songMetaChanged = true;
            }
            else if (stem.stemName == VocalsStemName)
            {
                songMeta.VocalsAudio = relativeOrAbsoluteOutputPath;
                songMetaChanged = true;
            }
        }
        
        // Update song meta
        if (songMetaChanged && saveSong)
        {
            songMetaManager.SaveSong(songMeta, true);
        }
    }

    private void OnInitializationStateChangedEvent(bool ready)
    {
        Debug.Log("Source separation state changed. ready: " + ready);
        isSourceSeparationModuleReady = ready;
    }

    private void OnError(string error)
    {
        Debug.LogError("Source separation failed: " + error);
    }

    private void OnSeparationReady(SourceSeparationComponent.SeparatedClipSet result)
    {
        Debug.Log($"Separation finished successfully. sourceName: '{result.sourceName}', model: {result.modelType}");
        foreach (SourceSeparationComponent.SeparatedStemClip stem in result.stems)
        {
            Debug.Log($"Stem: {stem.stemName}, Clip: {stem.clip.name}, Channels: {stem.channels}, SampleRate: {stem.sampleRate}, Length: {stem.clip.length} s");
        }
    }
}
