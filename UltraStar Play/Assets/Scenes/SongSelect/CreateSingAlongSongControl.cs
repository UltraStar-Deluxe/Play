using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using Vosk;

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
    private SpeechRecognitionManager speechRecognitionManager;

    private Job lastProcessSongJob;

    private readonly Subject<SongMeta> createdSingAlongVersionEventStream = new();
    public IObservable<SongMeta> CreatedSingAlongVersionEventStream => createdSingAlongVersionEventStream;

    public void OnInjectionFinished()
    {

    }

    public void CreateSingAlongSong(SongMeta songMeta)
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
        Job audioSeparationJob = new("Vocals separation", processSongJob);
        Job speechRecognitionJob = new("Speech recognition", processSongJob);
        Job pitchDetectionJob = new("Pitch detection", processSongJob);

        lastProcessSongJob = processSongJob;

        // (1) Run audio separation (vocals and instrumental audio)
        IObservable<AudioSeparationResult> audioSeparationObservable = audioSeparationManager.ProcessSongMeta(songMeta, audioSeparationJob);

        audioSeparationObservable
            .CatchIgnore((Exception ex) =>
            {
                audioSeparationJob.SetResult(EJobResult.Error);
            })
            .Subscribe(_ =>
            {
                audioSeparationJob.SetResult(EJobResult.Ok);
            });

        // Load speech recognition model in parallel while doing audio separation.
        string speechRecognitionModelPath = settings.SongEditorSettings.SpeechRecognitionModelPath;
        IObservable<object> loadSpeechRecognitionModelObservable = SpeechRecognitionUtils.LoadSpeechRecognitionModel(speechRecognitionModelPath, null);

        // Continue when audio separation and loading speech recognition model have finished
        Observable.WhenAll<object>(
                loadSpeechRecognitionModelObservable,
                audioSeparationObservable)
            .CatchIgnore((Exception ex) =>
            {
                speechRecognitionJob.SetResult(EJobResult.Error);
                pitchDetectionJob.SetResult(EJobResult.Error);
            })
            .Subscribe(_ =>
            {
                // (2) Run speech recognition on vocals audio

                // Load vocals audio
                AudioClip vocalsAudioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
                int lengthInBeats = (int)Math.Floor(vocalsAudioClip.length * BpmUtils.GetBeatsPerSecond(songMeta));

                SpeechRecognitionParameters speechRecognitionParameters = new(
                    vocalsAudioClip.frequency,
                    speechRecognitionModelPath,
                    SpeechRecognitionUtils.GetSpeechRecognitionPhrases(settings.SongEditorSettings.SpeechRecognitionPhrases));

                VoskRecognizer speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);

                float[] monoAudioSamples = AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, vocalsAudioClip, 0, lengthInBeats, true);

                SpeechRecognitionUtils.CreateNotesFromSpeechRecognition(
                        monoAudioSamples,
                        0,
                        monoAudioSamples.Length - 1,
                        vocalsAudioClip.frequency,
                        speechRecognitionParameters,
                        speechRecognitionJob,
                        speechRecognizer,
                        false,
                        settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                        songMeta,
                        0)
                    .CatchIgnore((Exception ex) =>
                    {
                        Debug.LogError(ex);
                        speechRecognitionJob.SetResult(EJobResult.Error);
                        pitchDetectionJob.SetResult(EJobResult.Error);
                        UiManager.CreateNotification(ex.Message);
                    })
                    .Subscribe(createdNotes =>
                    {
                        speechRecognitionJob.SetResult(EJobResult.Ok);

                        // (3) Split created notes into sentences and assign to first player.
                        SongMetaUtils.RemoveAllNotes(songMeta);
                        List<List<Note>> noteBatches = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, createdNotes);
                        noteBatches.ForEach(noteBatch =>
                            MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, noteBatch, Voice.firstVoiceName));

                        // (4) Add Space between notes
                        AddSpaceBetweenNotesUtils.AddSpaceBetweenNotes(createdNotes, 1);

                        // (5) Run pitch detection on vocals audio
                        pitchDetectionJob.SetStatus(EJobStatus.Running);
                        PitchDetectionUtils.MoveNotesToDetectedPitch(
                                songMeta,
                                createdNotes,
                                vocalsAudioClip,
                                settings.PitchDetectionAlgorithm.Value,
                                pitchDetectionJob)
                            .CatchIgnore((Exception ex) =>
                            {
                                pitchDetectionJob.SetResult(EJobResult.Error);
                            })
                            .Subscribe(_ =>
                            {
                                pitchDetectionJob.SetResult(EJobResult.Ok);

                                // (6) Save and reload song
                                songMetaManager.SaveSong(songMeta, true);
                                songMetaManager.ReloadSong(songMeta);

                                createdSingAlongVersionEventStream.OnNext(songMeta);
                            });
                    });
            });

        jobManager.AddJob(processSongJob);
    }
}
