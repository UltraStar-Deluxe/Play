using System;
using System.Collections.Generic;
using System.IO;
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
            Debug.LogError("Already processing a song");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            return null;
        }
        Debug.Log($"Creating sing-along data song '{songMeta.GetArtistDashTitle()}'");

        IJob parentJob = new Job<VoidEvent>(Translation.Get(R.Messages.job_createSingAlongDataWithName, "name", Path.GetFileName(songMeta.Audio)));
        lastProcessSongJob = parentJob;
        jobManager.AddJob(parentJob);

        try
        {
            // Load speech recognition model and run vocals isolation in parallel
            SpeechRecognitionParameters speechRecognitionParameters = await LoadSpeechRecognitionModelAndRunAudioSeparationAsync(songMeta, saveSongFile, parentJob);

            // Run speech recognition
            List<Note> createdNotes = await RunSpeechRecognitionAsync(songMeta, speechRecognitionParameters, parentJob);

            // Split created notes into sentences and assign to first player
            AssignNotesToFirstPlayer(songMeta, createdNotes);

            // Add Space between notes
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

            // Run pitch detection on vocals audio
            List<Note> loadedPitchDetectionNotes = await RunPitchDetectionAsync(songMeta, parentJob);

            // Move notes of first player to detected pitch
            MoveNotesToDetectedPitch(songMeta, createdNotes, loadedPitchDetectionNotes);

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

    private async Awaitable<SpeechRecognitionParameters> LoadSpeechRecognitionModelAndRunAudioSeparationAsync(
        SongMeta songMeta,
        bool saveSongFile,
        IJob parentJob)
    {
        // (0) Load speech recognition model
        SpeechRecognitionParameters speechRecognitionParameters = new(
            SettingsUtils.GetSpeechRecognitionModelPath(settings),
            SettingsUtils.GetSpeechRecognitionLanguage(settings),
            settings.SongEditorSettings.SpeechRecognitionPrompt);
        Awaitable<SpeechRecognizer> loadSpeechRecognizerAwaitable = SpeechRecognitionUtils.GetOrCreateSpeechRecognizerInJobAsync(speechRecognitionParameters);

        // (1) Run audio separation (vocals and instrumental audio)
        Awaitable<AudioSeparationResult> audioSeparationResultAwaitable = audioSeparationManager.ProcessSongMetaInJobAsync(songMeta, saveSongFile, parentJob);

        // Continue when audio separation and loading speech recognition model have finished both
        await Task.WhenAll(audioSeparationResultAwaitable.AsTask(), loadSpeechRecognizerAwaitable.AsTask());

        return speechRecognitionParameters;
    }

    private async Awaitable<List<Note>> RunSpeechRecognitionAsync(
        SongMeta songMeta,
        SpeechRecognitionParameters speechRecognitionParameters,
        IJob parentJob)
    {
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
            settings.SongEditorSettings.SpaceBetweenNotesInMillis,
            parentJob);

        return createdNotes;
    }

    private async Task<List<Note>> RunPitchDetectionAsync(SongMeta songMeta, IJob parentJob)
    {
        List<Note> loadedPitchDetectionNotes = await PitchDetectionUtils.CreateNotesUsingBasicPitchAsync(
            pitchDetectionManager,
            songMeta,
            parentJob);
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
