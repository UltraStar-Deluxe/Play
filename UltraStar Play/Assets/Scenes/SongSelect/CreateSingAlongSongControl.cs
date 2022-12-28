using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

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

    public void OnInjectionFinished()
    {

    }

    public void CreateSingAlongSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        Job processSongJob = new($"Create sing-along version of '{Path.GetFileName(songMeta.Mp3)}'");
        Job audioSeparationJob = new("Audio separation", processSongJob);
        Job speechRecognitionJob = new("Speech recognition", processSongJob);
        Job pitchDetectionJob = new("Pitch detection", processSongJob);

        // (1) Run audio separation (vocals and instrumental audio)
        IObservable<AudioSeparationResult> audioSeparationObservable = audioSeparationManager.ProcessSongMeta(songMeta, audioSeparationJob);
        audioSeparationObservable
            .CatchIgnore((Exception ex) =>
            {
                audioSeparationJob.SetResult(EJobResult.Error);
                speechRecognitionJob.SetResult(EJobResult.Error);
                pitchDetectionJob.SetResult(EJobResult.Error);
            })
            .Subscribe(_ =>
            {
                audioSeparationJob.SetResult(EJobResult.Ok);

                // (2) Run speech recognition on vocals audio

                // Load vocals audio
                AudioClip vocalsAudioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
                int lengthInBeats = (int)Math.Floor(vocalsAudioClip.length * BpmUtils.GetBeatsPerSecond(songMeta));

                VoskModelParameters voskModelParameters = new(
                    vocalsAudioClip.frequency,
                    settings.SongEditorSettings.SpeechRecognitionModelPath,
                    new List<string>());

                speechRecognitionJob.SetStatus(EJobStatus.Running);
                SpeechRecognitionUtils.CreateNotesFromSpeechRecognition(
                        songMeta,
                        vocalsAudioClip,
                        0,
                        lengthInBeats,
                        voskModelParameters,
                        settings.SongEditorSettings.MidiNoteForSpeechRecognition,
                        speechRecognitionJob)
                    .CatchIgnore((Exception ex) =>
                    {
                        speechRecognitionJob.SetResult(EJobResult.Error);
                        pitchDetectionJob.SetResult(EJobResult.Error);
                    })
                    .Subscribe(createdNotes =>
                    {
                        speechRecognitionJob.SetResult(EJobResult.Ok);

                        // (3) Assign created notes to first player.
                        SongMetaUtils.RemoveAllNotes(songMeta);
                        MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, createdNotes, Voice.firstVoiceName);

                        // (4) Run pitch detection on vocals audio
                        pitchDetectionJob.SetStatus(EJobStatus.Running);
                        PitchDetectionUtils.MoveNotesToDetectedPitch(
                                songMeta,
                                createdNotes,
                                vocalsAudioClip,
                                settings.PitchDetectionAlgorithm,
                                pitchDetectionJob)
                            .CatchIgnore((Exception ex) =>
                            {
                                pitchDetectionJob.SetResult(EJobResult.Error);
                            })
                            .Subscribe(_ =>
                            {
                                // (5) Save and reload song
                                songMetaManager.SaveSong(songMeta, true);
                                songMetaManager.ReloadSong(songMeta);

                                // TODO: fire event to update song select UI entries
                                uiManager.CreateNotificationVisualElement($"Created sing-along version of '{Path.GetFileName(songMeta.Mp3)}'");

                                pitchDetectionJob.SetResult(EJobResult.Ok);
                            });
                    });
            });

        jobManager.AddJob(processSongJob);
    }
}
