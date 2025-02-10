using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreateSingAlongSongControl : INeedInjection
{
    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private PitchDetectionManager pitchDetectionManager;

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    private IJob lastProcessSongJob;

    private readonly Subject<SongMeta> createdSingAlongVersionEventStream = new();
    public IObservable<SongMeta> CreatedSingAlongVersionEventStream => createdSingAlongVersionEventStream;

    public async void CreateSingAlongSong(SongMeta songMeta, bool saveSongFile)
    {
        await CreateSingAlongSongAsync(songMeta, saveSongFile);
    }

    public async Awaitable<SongMeta> CreateSingAlongSongAsync(SongMeta songMeta, bool saveSongFile)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException(nameof(songMeta));
        }

        if (lastProcessSongJob != null
            && lastProcessSongJob.Result.Value == EJobResult.Pending)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            throw new JobAlreadyRunningException("Already creating sing along data for another song");
        }
        Debug.Log($"Creating sing-along data for song '{songMeta.GetArtistDashTitle()}'");

        try
        {
            IJob parentJob = CreateSingAlongDataJobPipeline(songMeta, saveSongFile);
            jobManager.AddJob(parentJob);

            await parentJob.RunAsync();

            // Save
            if (saveSongFile)
            {
                SaveAndReloadSong(songMeta);
            }

            return songMeta;
        }
        catch (Exception ex)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
            throw ex;
        }
    }

    private IJob CreateSingAlongDataJobPipeline(SongMeta songMeta, bool saveSongFile)
    {
        IJob parentJob = new Job<VoidEvent>(Translation.Get(R.Messages.job_createSingAlongDataWithName,
            "name", Path.GetFileName(songMeta.Audio)));
        lastProcessSongJob = parentJob;

        // Run vocals isolation
        Job<AudioSeparationResult> audioSeparationJob = new(Translation.Of("Vocals isolation"), new CancellationTokenSource());
        audioSeparationJob.SetAwaitable(() => RunVocalsIsolationAsync(songMeta, saveSongFile, audioSeparationJob));
        parentJob.AddChildJob(audioSeparationJob);

        // Run speech recognition
        List<Note> createdNotes = new();
        // TODO: How to include child jobs for creating speech recognizer and doing speech recognition?
        Job<VoidEvent> createSpeechRecognizerAndRunSpeechRecognitionJob = new(Translation.Of("Speech recognition"), new CancellationTokenSource());
        createSpeechRecognizerAndRunSpeechRecognitionJob.SetAwaitable(async () =>
            {
                createdNotes = await RunSpeechRecognitionAsync(songMeta);

                // Split created notes into sentences and assign to first player
                AssignNotesToFirstPlayer(songMeta, createdNotes);

                // Add Space between notes
                SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

                return VoidEvent.instance;
            });
        parentJob.AddChildJob(createSpeechRecognizerAndRunSpeechRecognitionJob);

        // Run pitch detection on vocals audio
        // TODO: include child job to run pitch detection?
        List<Note> loadedPitchDetectionNotes;
        Job<VoidEvent> pitchDetectionJob = new Job<VoidEvent>(Translation.Of("Run pitch detection"));
        pitchDetectionJob.SetAwaitable(async () =>
        {
            loadedPitchDetectionNotes = await RunPitchDetectionAsync(songMeta);

            // Move notes of first player to detected pitch
            MoveNotesToDetectedPitch(songMeta, createdNotes, loadedPitchDetectionNotes);

            return VoidEvent.instance;
        });
        parentJob.AddChildJob(pitchDetectionJob);

        return parentJob;
    }

    private static void MoveNotesToDetectedPitch(SongMeta songMeta, List<Note> createdNotes, List<Note> loadedPitchDetectionNotes)
    {
        try
        {
            PitchDetectionUtils.MoveNotesToDetectedPitchUsingPitchDetectionLayer(
                songMeta,
                createdNotes,
                loadedPitchDetectionNotes);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to move notes to detected pitch");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
        }
    }

    private async Awaitable<AudioSeparationResult> RunVocalsIsolationAsync(
        SongMeta songMeta,
        bool saveSongFile,
        Job<AudioSeparationResult> audioSeparationJob)
    {
        return await audioSeparationManager.ProcessSongMetaInJobAsync(songMeta, saveSongFile, audioSeparationJob);
    }

    private async Awaitable<List<Note>> RunSpeechRecognitionAsync(
        SongMeta songMeta)
    {
        // Load speech recognition model
        SpeechRecognitionParameters speechRecognitionParameters = new(
            SettingsUtils.GetSpeechRecognitionModelPath(settings),
            SettingsUtils.GetSpeechRecognitionLanguage(settings),
            settings.SongEditorSettings.SpeechRecognitionPrompt);

        // Load vocals audio
        AudioClip vocalsAudioClip = await AudioManager.LoadAudioClipFromUriAsync(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
        int lengthInBeats = (int)Math.Floor(vocalsAudioClip.length * SongMetaBpmUtils.BeatsPerSecond(songMeta));

        float[] monoAudioSamples = AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, vocalsAudioClip, 0, lengthInBeats, true);

        List<Note> createdNotes = await SpeechRecognitionUtils.CreateNotesFromSpeechRecognitionAsync(
            monoAudioSamples,
            0,
            monoAudioSamples.Length - 1,
            vocalsAudioClip.frequency,
            speechRecognitionParameters,
            settings.SongEditorSettings.DefaultPitchForCreatedNotes,
            songMeta,
            0,
            SettingsUtils.CreateHyphenator(settings),
            settings.SongEditorSettings.SpaceBetweenNotesInMillis);

        return createdNotes;
    }

    private async Task<List<Note>> RunPitchDetectionAsync(SongMeta songMeta)
    {
        List<Note> loadedPitchDetectionNotes = await PitchDetectionUtils.CreateNotesUsingBasicPitchAsync(
            pitchDetectionManager,
            songMeta);
        if (loadedPitchDetectionNotes.IsNullOrEmpty())
        {
            throw new PitchDetectionException("Failed to load pitch detection result");
        }

        return loadedPitchDetectionNotes;
    }

    private void SaveAndReloadSong(SongMeta songMeta)
    {
        try
        {
            songMetaManager.SaveSong(songMeta, true);
            songMetaManager.ReloadSong(songMeta);

            createdSingAlongVersionEventStream.OnNext(songMeta);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to save song with sing-along data");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
        }
    }

    private static void AssignNotesToFirstPlayer(SongMeta songMeta, List<Note> createdNotes)
    {
        SongMetaUtils.RemoveAllNotes(songMeta);
        List<List<Note>> noteBatches = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, createdNotes);
        noteBatches.ForEach(noteBatch => MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, noteBatch, EVoiceId.P1));
    }
}
