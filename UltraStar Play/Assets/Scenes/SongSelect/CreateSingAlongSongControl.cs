using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using Whisper;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreateSingAlongSongControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private AudioManager audioManager;

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

    public void OnInjectionFinished()
    {

    }

    public void CreateSingAlongSong(SongMeta songMeta, bool saveSongFile)
    {
        if (songMeta == null)
        {
            return;
        }

        if (lastProcessSongJob != null
            && lastProcessSongJob.Result.Value == EJobResult.Pending)
        {
            UiManager.CreateNotification("Already processing a song.\nWait until the running tasks have finished.");
            return;
        }

        Job processSongJob = new($"Create sing-along version of '{Path.GetFileName(songMeta.Mp3)}'");
        Job audioSeparationJob = new("Vocals isolation", processSongJob);
        Job speechRecognitionJob = new("Speech recognition", processSongJob);
        Job pitchDetectionJob = new("Pitch detection", processSongJob);

        lastProcessSongJob = processSongJob;

        // (1) Run audio separation (vocals and instrumental audio)
        IObservable<AudioSeparationResult> audioSeparationObservable = audioSeparationManager.ProcessSongMetaAsObservable(songMeta, audioSeparationJob);

        // audioSeparationObservable
        //     .CatchIgnore((Exception ex) =>
        //     {
        //         audioSeparationJob.SetResult(EJobResult.Error);
        //     })
        //     .Subscribe(evt =>
        //     {
        //         Debug.Log($"Successfully separated audio: {evt}");
        //         audioSeparationJob.SetResult(EJobResult.Ok);
        //     });

        SpeechRecognitionParameters speechRecognitionParameters = new(
            settings.SongEditorSettings.SpeechRecognitionModelPath,
            "auto",
            settings.SongEditorSettings.SpeechRecognitionPrompt);
        
        // Load speech recognition model in parallel while doing audio separation.
        IObservable<SpeechRecognizer> loadSpeechRecognizerObservable = SpeechRecognitionUtils.GetOrCreateSpeechRecognizerAsObservable(speechRecognitionParameters, null);

        // Outer scope reference to variables that are used in multiple steps
        List<Note> createdNotes = new List<Note>();

        // Continue when audio separation and loading speech recognition model have finished
        Observable.WhenAll<object>(
                loadSpeechRecognizerObservable,
                audioSeparationObservable)
            .CatchIgnore((Exception ex) =>
            {
                audioSeparationJob.SetResult(EJobResult.Error);
                speechRecognitionJob.SetResult(EJobResult.Error);
                pitchDetectionJob.SetResult(EJobResult.Error);
                UiManager.CreateNotification($"Failed to create sing-along data.\n{ex.Message}");
            })
            .SelectMany(_ =>
            {
                audioSeparationJob.SetResult(EJobResult.Ok);

                // (2) Run speech recognition on vocals audio

                // Load vocals audio
                AudioClip vocalsAudioClip = audioManager.LoadAudioClipFromUriImmediately(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
                int lengthInBeats = (int)Math.Floor(vocalsAudioClip.length * BpmUtils.GetBeatsPerSecond(songMeta));
                    
                float[] monoAudioSamples = AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, vocalsAudioClip, 0, lengthInBeats, true);

                return SpeechRecognitionUtils.CreateNotesFromSpeechRecognitionAsObservable(
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
            })
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Create sing-along song failed: {ex.Message}");
                speechRecognitionJob.SetResult(EJobResult.Error);
                pitchDetectionJob.SetResult(EJobResult.Error);
                UiManager.CreateNotification(ex.Message);
            })
            .SelectMany(localCreatedNotes =>
            {
                createdNotes = localCreatedNotes;
                speechRecognitionJob.SetResult(EJobResult.Ok);

                // (3) Split created notes into sentences and assign to first player.
                SongMetaUtils.RemoveAllNotes(songMeta);
                List<List<Note>> noteBatches = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, createdNotes);
                noteBatches.ForEach(noteBatch =>
                    MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, noteBatch, Voice.firstVoiceName, false));

                // (4) Add Space between notes
                SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

                // (5) Run pitch detection on vocals audio
                pitchDetectionJob.SetStatus(EJobStatus.Running);
                return PitchDetectionUtils.CreateNotesUsingBasicPitch(
                    pitchDetectionManager,
                    songMeta,
                    pitchDetectionJob);
            })
            .CatchIgnore((Exception ex) =>
            {
                pitchDetectionJob.SetResult(EJobResult.Error);
                Debug.LogException(ex);
                string localErrorMessage = $"Pitch detection failed.";
                Debug.LogError(localErrorMessage);
                UiManager.CreateNotification(localErrorMessage);
            })
            .Subscribe(loadedPitchDetectionNotes =>
            {
                if (loadedPitchDetectionNotes.IsNullOrEmpty())
                {
                    pitchDetectionJob.SetResult(EJobResult.Error);
                    string localErrorMessage = "Failed to load pitch detection result.";
                    Debug.LogError(localErrorMessage);
                    UiManager.CreateNotification(localErrorMessage);
                    return;
                }

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
                    string localErrorMessage = "Failed to move notes to detected pitch";
                    Debug.LogError(localErrorMessage);
                    UiManager.CreateNotification(localErrorMessage);
                }

                try
                {
                    if (saveSongFile)
                    {
                        // (6) Save and reload song
                        songMetaManager.SaveSong(songMeta, true);
                        songMetaManager.ReloadSong(songMeta);
                    }
                    createdSingAlongVersionEventStream.OnNext(songMeta);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    string localErrorMessage = "Failed to save song with sing-along data";
                    Debug.LogError(localErrorMessage);
                    UiManager.CreateNotification(localErrorMessage);
                }
            });

        jobManager.AddJob(processSongJob);
    }
}
