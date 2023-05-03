using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneAlternativeAudioPlayer : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public AudioSource instrumentalAudioSource;

    [InjectedInInspector]
    public AudioSource vocalsAudioSource;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private AudioManager audioManager;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SingSceneModifierControl modifierControl;

    [Inject]
    private SingSceneAudioFadeInControl audioFadeInControl;

    private bool hasLoadedInstrumentalAndVocalsAudio;

    private bool isInitialized;

    private void Start()
    {
        audioSeparationManager.AudioSeparationFinishedEventStream
            .Subscribe(evt =>
            {
                if (evt.SongMeta == songMeta)
                {
                    Init();
                }
            })
            .AddTo(gameObject);

        modifierControl.ModifiedVolumePercent.Subscribe(_ => UpdateAudioSources());
        audioFadeInControl.FadeInVolumePercent.Subscribe(_ => UpdateAudioSources());

        Init();
    }

    private void Init()
    {
        if (isInitialized
            || !CanPlayInstrumentalAndVocalsAudio(out string errorMessage))
        {
            return;
        }

        songAudioPlayer.PlaybackStartedEventStream.Subscribe(_ =>
        {
            PlayInstrumentalAndVocalsAudio();
            SyncAudioPosition();
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(_ => PauseInstrumentalAndVocalsAudio());
        songAudioPlayer.JumpForwardInSongEventStream.Subscribe(_ => SyncAudioPosition());
        songAudioPlayer.JumpBackInSongEventStream.Subscribe(_ => SyncAudioPosition());

        UpdateAudioSources();
        settings.VocalsAudioVolumePercent
            .Subscribe(_ => UpdateAudioSources());

        isInitialized = true;
    }

    private void PauseInstrumentalAndVocalsAudio()
    {
        if (instrumentalAudioSource.clip != null)
        {
            instrumentalAudioSource.Pause();
        }

        if (vocalsAudioSource.clip != null)
        {
            vocalsAudioSource.Pause();
        }
    }

    private void PlayInstrumentalAndVocalsAudio()
    {
        if (instrumentalAudioSource.clip != null
            && vocalsAudioSource.clip != null
            && (!instrumentalAudioSource.isPlaying
                || !vocalsAudioSource.isPlaying))
        {
            instrumentalAudioSource.Play();
            vocalsAudioSource.Play();
            SyncAudioPosition();
        }
    }

    private void SyncAudioPosition()
    {
        float songAudioPlayerTimeInSeconds = (float)songAudioPlayer.PositionInSongInSeconds;
        instrumentalAudioSource.time = songAudioPlayerTimeInSeconds;
        vocalsAudioSource.time = songAudioPlayerTimeInSeconds;
    }

    public void UpdateAudioSources()
    {
        if (settings.VocalsAudioVolumePercent.Value >= 100
            || !CanPlayInstrumentalAndVocalsAudio(out string errorMessage))
        {
            UseOriginalSongAudio();
        }
        else
        {
            UseInstrumentalAndVocalsAudio();
        }
    }

    private void UseOriginalSongAudio()
    {
        songAudioPlayer.VolumeFactor = NumberUtils.PercentToFactor(settings.VolumePercent.Value)
                                             * NumberUtils.PercentToFactor(modifierControl.ModifiedVolumePercent.Value)
                                             * NumberUtils.PercentToFactor(audioFadeInControl.FadeInVolumePercent.Value);
        vocalsAudioSource.volume = 0;
        instrumentalAudioSource.volume = 0;

        PauseInstrumentalAndVocalsAudio();
    }

    private void UseInstrumentalAndVocalsAudio()
    {
        if (!hasLoadedInstrumentalAndVocalsAudio)
        {
            hasLoadedInstrumentalAndVocalsAudio = true;
            instrumentalAudioSource.clip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetInstrumentalAudioUri(songMeta));
            vocalsAudioSource.clip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetVocalsAudioUri(songMeta));
        }

        songAudioPlayer.VolumeFactor = 0;
        instrumentalAudioSource.volume = NumberUtils.PercentToFactor(settings.VolumePercent.Value)
                                         * NumberUtils.PercentToFactor(modifierControl.ModifiedVolumePercent.Value)
                                         * NumberUtils.PercentToFactor(audioFadeInControl.FadeInVolumePercent.Value);
        vocalsAudioSource.volume = NumberUtils.PercentToFactor(settings.VolumePercent.Value)
                                   * NumberUtils.PercentToFactor(settings.VocalsAudioVolumePercent.Value)
                                   * NumberUtils.PercentToFactor(modifierControl.ModifiedVolumePercent.Value)
                                   * NumberUtils.PercentToFactor(audioFadeInControl.FadeInVolumePercent.Value);

        if (songAudioPlayer.IsPlaying)
        {
            PlayInstrumentalAndVocalsAudio();
        }
    }

    private bool CanPlayInstrumentalAndVocalsAudio(out string errorMessage)
    {
        if (songMeta.VocalsAudio.IsNullOrEmpty())
        {
            errorMessage = "No vocals audio found. Separate the audio first.";
            return false;
        }
        if (!SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            errorMessage = $"Resource does not exist: {songMeta.VocalsAudio}";
            return false;
        }

        if (songMeta.InstrumentalAudio.IsNullOrEmpty())
        {
            errorMessage = "No instrumental audio found. Separate the audio first.";
            return false;
        }
        if (!SongMetaUtils.InstrumentalAudioResourceExists(songMeta))
        {
            errorMessage = $"File does not exist: {songMeta.VocalsAudio}";
            return false;
        }

        errorMessage = "";
        return true;
    }
}
