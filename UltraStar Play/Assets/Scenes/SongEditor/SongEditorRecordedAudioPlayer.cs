using System;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorRecordedAudioPlayer : MonoBehaviour, INeedInjection
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
    private Settings settings;

    private void Start()
    {
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(_ =>
        {
            if (settings.SongEditorSettings.PlayRecordedSamples
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
        if (settings.SongEditorSettings.PlayRecordedSamples)
        {
            AudioSource.volume = settings.AudioSettings.VolumePercent / 100f;
            songAudioPlayer.audioPlayer.volume = 0;
        }
        else
        {
            AudioSource.volume = 0;
            songAudioPlayer.audioPlayer.volume = settings.AudioSettings.VolumePercent / 100f;
        }
    }
}
