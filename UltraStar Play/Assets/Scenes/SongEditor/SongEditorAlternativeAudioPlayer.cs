using System;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorAlternativeAudioPlayer : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public AudioSource AudioSource { get; private set; }

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSampleRecorderControl sampleRecorderControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private AudioManager audioManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    private void Start()
    {
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(_ =>
        {
            if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Recording
                && !sampleRecorderControl.HasRecordedAudio)
            {
                uiManager.CreateNotificationVisualElement("No recorded audio. Use a microphone to record audio first.");
            }
            AudioSource.Play();
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(_ => AudioSource.Pause());
        songAudioPlayer.JumpBackInSongEventStream.Subscribe(_ => AudioSource.time = (float)songAudioPlayer.PositionInSongInSeconds);
        songAudioPlayer.JumpForwardInSongEventStream.Subscribe(_ => AudioSource.time = (float)songAudioPlayer.PositionInSongInSeconds);
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(_ => AudioSource.Pause());
        songAudioPlayer.PositionInSongEventStream.Subscribe(_ =>
        {
            if (!songAudioPlayer.IsPlaying)
            {
                AudioSource.time = (float)songAudioPlayer.PositionInSongInSeconds;
            }
        });
    }

    private void Update()
    {
        SelectAudioClip();
        UpdateVolume();
    }

    private void UpdateVolume()
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.OriginalMusic)
        {
            AudioSource.volume = 0;
            songAudioPlayer.audioPlayer.volume = settings.AudioSettings.VolumePercent / 100f;
        }
        else
        {
            AudioSource.volume = settings.AudioSettings.VolumePercent / 100f;
            songAudioPlayer.audioPlayer.volume = 0;
        }
    }

    private void SelectAudioClip()
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.OriginalMusic)
        {
            return;
        }

        AudioClip GetAudioClip()
        {
            switch (settings.SongEditorSettings.PlaybackSamplesSource)
            {
                case ESongEditorSamplesSource.Recording:
                    return sampleRecorderControl.AudioClip;
                case ESongEditorSamplesSource.Vocals:
                    return audioManager.LoadAudioClipFromUri(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
                case ESongEditorSamplesSource.Instrumental:
                    return audioManager.LoadAudioClipFromUri(SongMetaUtils.GetInstrumentalAudioUri(songMeta), false);
                default:
                    return null;
            }
        }

        AudioClip targetAudioClip = GetAudioClip();
        if (AudioSource.clip != targetAudioClip)
        {
            AudioSource.Stop();
            AudioSource.clip = targetAudioClip;
            AudioSource.time = songAudioPlayer.audioPlayer.time;
            AudioSource.Play();
        }
    }
}
