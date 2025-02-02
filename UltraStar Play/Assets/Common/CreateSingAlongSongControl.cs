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

    private Job lastProcessSongJob;

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

        Job processSongJob = new(Translation.Get(R.Messages.job_createSingAlongDataWithName, "name", Path.GetFileName(songMeta.Audio)));
        Job audioSeparationJob = new(Translation.Get(R.Messages.job_audioSeparation), processSongJob);
        Job speechRecognitionJob = new(Translation.Get(R.Messages.job_speechRecognition), processSongJob);
        Job pitchDetectionJob = new(Translation.Get(R.Messages.job_pitchDetection), processSongJob);

        lastProcessSongJob = processSongJob;

        jobManager.AddJob(processSongJob);

        try
        {
            // Load speech recognition model and run vocals isolation in parallel
            SpeechRecognitionParameters speechRecognitionParameters = await LoadSpeechRecognitionModelAndRunAudioSeparationAsync(songMeta, saveSongFile, audioSeparationJob);

            // Run speech recognition
            List<Note> createdNotes = await RunSpeechRecognitionAsync(songMeta, speechRecognitionParameters, speechRecognitionJob);

            // Split created notes into sentences and assign to first player
            AssignNotesToFirstPlayer(songMeta, createdNotes);

            // Add Space between notes
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

            // Run pitch detection on vocals audio
            List<Note> loadedPitchDetectionNotes = await RunPitchDetectionAsync(songMeta, pitchDetectionJob);

            // Move notes of first player to detected pitch
            MoveNotesToDetectedPitch(songMeta, createdNotes, loadedPitchDetectionNotes, pitchDetectionJob);

            // Save
            if (saveSongFile)
            {
                SaveAndReloadSong(songMeta);
            }

            return songMeta;
        }
        catch (Exception ex)
        {
            audioSeparationJob?.SetResult(EJobResult.Error);
            speechRecognitionJob?.SetResult(EJobResult.Error);
            pitchDetectionJob?.SetResult(EJobResult.Error);
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));

            ExceptionUtils.LogThenThrow(new Exception($"Failed to create sing-along data", ex));
            throw ex;
        }
    }

    private static void MoveNotesToDetectedPitch(SongMeta songMeta, List<Note> createdNotes, List<Note> loadedPitchDetectionNotes, Job pitchDetectionJob)
    {
        try
        {
            PitchDetectionUtils.MoveNotesToDetectedPitchUsingPitchDetectionLayer(
                songMeta,
                createdNotes,
                loadedPitchDetectionNotes);
            pitchDetectionJob.SetResult(EJobResult.Ok);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to move notes to detected pitch");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
        }
    }

    private async Awaitable<SpeechRecognitionParameters> LoadSpeechRecognitionModelAndRunAudioSeparationAsync(SongMeta songMeta, bool saveSongFile, Job audioSeparationJob)
    {
        // (0) Load speech recognition model
        SpeechRecognitionParameters speechRecognitionParameters = new(
            SettingsUtils.GetSpeechRecognitionModelPath(settings),
            SettingsUtils.GetSpeechRecognitionLanguage(settings),
            settings.SongEditorSettings.SpeechRecognitionPrompt);
        Awaitable<SpeechRecognizer> loadSpeechRecognizerAwaitable = SpeechRecognitionUtils.GetOrCreateSpeechRecognizerAsync(speechRecognitionParameters, null);

        // (1) Run audio separation (vocals and instrumental audio)
        Awaitable<AudioSeparationResult> audioSeparationResultAwaitable = audioSeparationManager.ProcessSongMetaAsync(songMeta, saveSongFile, audioSeparationJob);

        // Continue when audio separation and loading speech recognition model have finished both
        await Task.WhenAll(audioSeparationResultAwaitable.AsTask(), loadSpeechRecognizerAwaitable.AsTask());

        audioSeparationJob.SetResult(EJobResult.Ok);

        return speechRecognitionParameters;
    }

    private async Awaitable<List<Note>> RunSpeechRecognitionAsync(SongMeta songMeta, SpeechRecognitionParameters speechRecognitionParameters, Job speechRecognitionJob)
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
            speechRecognitionJob,
            false,
            settings.SongEditorSettings.DefaultPitchForCreatedNotes,
            songMeta,
            0,
            SettingsUtils.CreateHyphenator(settings),
            settings.SongEditorSettings.SpaceBetweenNotesInMillis);

        speechRecognitionJob.SetResult(EJobResult.Ok);

        return createdNotes;
    }

    private async Task<List<Note>> RunPitchDetectionAsync(SongMeta songMeta, Job pitchDetectionJob)
    {
        pitchDetectionJob.SetStatus(EJobStatus.Running);
        List<Note> loadedPitchDetectionNotes = await PitchDetectionUtils.CreateNotesUsingBasicPitchAsync(
            pitchDetectionManager,
            songMeta,
            pitchDetectionJob);

        if (loadedPitchDetectionNotes.IsNullOrEmpty())
        {
            pitchDetectionJob.SetResult(EJobResult.Error);
            Debug.LogError($"Failed to load pitch detection result");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
            return loadedPitchDetectionNotes;
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
