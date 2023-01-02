using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.PlayerLoop;

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

        Init();
    }

    private void Init()
    {
        if (isInitialized
            || !CanPlayAudio(out string errorMessage))
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
        settings.ObserveEveryValueChanged(it => it.AudioSettings.VocalsAudioVolumePercent)
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
        float songAudioPlayerTimeInSeconds = songAudioPlayer.audioPlayer.time;
        instrumentalAudioSource.time = songAudioPlayerTimeInSeconds;
        vocalsAudioSource.time = songAudioPlayerTimeInSeconds;
    }

    private void UpdateAudioSources()
    {
        if (settings.AudioSettings.VocalsAudioVolumePercent >= 100)
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
        songAudioPlayer.audioPlayer.volume = NumberUtils.PercentToFactor(settings.AudioSettings.VolumePercent);
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

        songAudioPlayer.audioPlayer.volume = 0;
        instrumentalAudioSource.volume = NumberUtils.PercentToFactor(settings.AudioSettings.VolumePercent);
        vocalsAudioSource.volume = NumberUtils.PercentToFactor(settings.AudioSettings.VolumePercent)
                                   * NumberUtils.PercentToFactor(settings.AudioSettings.VocalsAudioVolumePercent);

        if (songAudioPlayer.IsPlaying)
        {
            PlayInstrumentalAndVocalsAudio();
        }
    }

    private bool CanPlayAudio(out string errorMessage)
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
