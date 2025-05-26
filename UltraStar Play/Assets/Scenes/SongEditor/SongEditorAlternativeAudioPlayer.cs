using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorAlternativeAudioPlayer : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public AudioSource AudioSource { get; private set; }

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorMicSampleRecorder micSampleRecorder;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    private readonly HashSet<string> failedAudioClipPaths = new();

    private void Start()
    {
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(_ =>
        {
            if (!CanPlayAudio(out Translation errorMessage))
            {
                NotificationManager.CreateNotification(errorMessage);
                return;
            }
            AudioSource.Play();
        });
        songAudioPlayer.JumpBackEventStream.Subscribe(_ =>
        {
            if (AudioSource.clip == null)
            {
                return;
            }
            AudioSource.time = (float)songAudioPlayer.PositionInSeconds;
        });
        songAudioPlayer.JumpForwardEventStream.Subscribe(_ =>
        {
            if (AudioSource.clip == null)
            {
                return;
            }
            AudioSource.time = (float)songAudioPlayer.PositionInSeconds;
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(_ =>
        {
            if (AudioSource.clip == null)
            {
                return;
            }

            AudioSource.Pause();
        });
        songAudioPlayer.PlaybackSpeedChangedEventStream.Subscribe(newValue => PitchShifterUtils.SetPitchWithPitchShifter(AudioSource, (float)newValue));

        // Update AudioClip (instrumental, vocals) when corresponding settings change
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource)
            .Subscribe(_ => UpdateAudioClip());

        // Update music volume (instrumental, vocals, songAudioPlayer) when corresponding settings change
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.PlaybackSamplesSource)
            .Subscribe(_ => UpdateVolume());
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.MusicVolumePercent)
            .Subscribe(_ => UpdateVolume());

        songAudioPlayer.PlaybackStartedEventStream
            .Subscribe(_ =>
            {
                UpdateVolume();
                UpdateAudioClip();
            });
    }

    private void Update()
    {
        SynchronizePositionWithSongAudioPlayer();
    }

    private void SynchronizePositionWithSongAudioPlayer()
    {
        if (!songAudioPlayer.IsPlaying)
        {
            return;
        }

        double targetPositionInMillis = songAudioPlayer.PositionInMillis;
        double actualPositionInMillis = AudioSource.time * 1000;
        double positionDifferenceInMillis = targetPositionInMillis - actualPositionInMillis;
        double positionDistanceInMillis = Math.Abs(positionDifferenceInMillis);
        if (positionDistanceInMillis > 1200)
        {
            // Re-Synchronize
            AudioSource.time = (float)(targetPositionInMillis / 1000);
        }
    }

    private void UpdateVolume()
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.OriginalMusic)
        {
            AudioSource.volume = 0;
            songAudioPlayer.VolumeFactor = NumberUtils.PercentToFactor(settings.SongEditorSettings.MusicVolumePercent);
        }
        else
        {
            AudioSource.volume = NumberUtils.PercentToFactor(settings.SongEditorSettings.MusicVolumePercent);
            songAudioPlayer.VolumeFactor = 0;
        }
    }

    private async void UpdateAudioClip()
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.OriginalMusic)
        {
            return;
        }

        AudioClip targetAudioClip = await LoadAudioClip();
        if (targetAudioClip == null)
        {
            AudioSource.Stop();
            AudioSource.clip = null;
            return;
        }

        if (AudioSource.clip != targetAudioClip)
        {
            AudioSource.Stop();
            AudioSource.clip = targetAudioClip;
            AudioSource.time = (float)songAudioPlayer.PositionInSeconds;

            if (songAudioPlayer.IsPlaying)
            {
                AudioSource.Play();
            }
        }
    }

    private async Awaitable<AudioClip> LoadAudioClip()
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Recording)
        {
            return micSampleRecorder.AudioClip;
        }

        string audioClipUri;
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Vocals)
        {
            audioClipUri = SongMetaUtils.GetVocalsAudioUri(songMeta);
        }
        else if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Instrumental)
        {
            audioClipUri = SongMetaUtils.GetInstrumentalAudioUri(songMeta);
        }
        else
        {
            return null;
        }

        if (failedAudioClipPaths.Contains(audioClipUri))
        {
            // Do not attempt to load this clip again.
            return null;
        }

        AudioClip loadedAudioClip = await AudioManager.LoadAudioClipFromUriAsync(audioClipUri, false);
        if (loadedAudioClip == null)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_failedToLoadWithName,
                "name", audioClipUri));
            failedAudioClipPaths.Add(audioClipUri);
        }

        return loadedAudioClip;
    }

    private bool CanPlayAudio(out Translation errorMessage)
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Recording
            && !micSampleRecorder.HasRecordedAudio)
        {
            errorMessage = Translation.Get(R.Messages.songEditor_error_missingRecordedAudio);
            return false;
        }
        else if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Vocals)
        {
            if (songMeta.VocalsAudio.IsNullOrEmpty())
            {
                errorMessage = Translation.Get(R.Messages.songEditor_error_missingVocalsAudio);
                return false;
            }

            if (!WebRequestUtils.IsHttpOrHttpsUri(songMeta.VocalsAudio)
                && !File.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio)))
            {
                errorMessage = Translation.Get(R.Messages.common_error_fileNotFoundWithName,
                    "name", songMeta.VocalsAudio);
                return false;
            }
        }
        else if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Instrumental)
        {
            if (songMeta.InstrumentalAudio.IsNullOrEmpty())
            {
                errorMessage = Translation.Get(R.Messages.songEditor_error_missingInstrumentalAudio);
                return false;
            }

            if (!WebRequestUtils.IsHttpOrHttpsUri(songMeta.InstrumentalAudio)
                && !File.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.InstrumentalAudio)))
            {
                errorMessage = Translation.Get(R.Messages.common_error_fileNotFoundWithName,
                    "name", songMeta.VocalsAudio);
                return false;
            }
        }

        errorMessage = Translation.Empty;
        return true;
    }
}
