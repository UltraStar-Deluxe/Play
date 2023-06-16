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
    private AudioManager audioManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    private readonly HashSet<string> failedAudioClipPaths = new();

    private void Start()
    {
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(_ =>
        {
            if (!CanPlayAudio(out string errorMessage))
            {
                UiManager.CreateNotification(errorMessage);
                return;
            }
            AudioSource.Play();
        });
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
        songAudioPlayer.PlaybackSpeedChangedEventStream.Subscribe(newValue => AudioUtils.SetPitchWithPitchShifter(AudioSource, newValue));
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
            songAudioPlayer.VolumeFactor = NumberUtils.PercentToFactor(settings.SongEditorSettings.MusicVolumePercent);
        }
        else
        {
            AudioSource.volume = NumberUtils.PercentToFactor(settings.SongEditorSettings.MusicVolumePercent);
            songAudioPlayer.VolumeFactor = 0;
        }
    }

    private void SelectAudioClip()
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.OriginalMusic)
        {
            return;
        }

        AudioClip targetAudioClip = LoadAudioClip();
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
            AudioSource.time = (float)songAudioPlayer.PositionInSongInSeconds;

            if (songAudioPlayer.IsPlaying)
            {
                AudioSource.Play();
            }
        }
    }

    private AudioClip LoadAudioClip()
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

        AudioClip loadedAudioClip = audioManager.LoadAudioClipFromUriImmediately(audioClipUri, false);
        if (loadedAudioClip == null)
        {
            UiManager.CreateNotification($"Failed to load {audioClipUri}");
            failedAudioClipPaths.Add(audioClipUri);
        }

        return loadedAudioClip;
    }

    private bool CanPlayAudio(out string errorMessage)
    {
        if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Recording
            && !micSampleRecorder.HasRecordedAudio)
        {
            errorMessage = "Cannot play recorded audio. Use a microphone to record audio first.";
            return false;
        }
        else if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Vocals)
        {
            if (songMeta.VocalsAudio.IsNullOrEmpty())
            {
                errorMessage = "No vocals audio found. Split the audio first.";
                return false;
            }

            if (!WebRequestUtils.IsHttpOrHttpsUri(songMeta.VocalsAudio)
                && !File.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio)))
            {
                errorMessage = $"File does not exist: {songMeta.VocalsAudio}";
                return false;
            }
        }
        else if (settings.SongEditorSettings.PlaybackSamplesSource == ESongEditorSamplesSource.Instrumental)
        {
            if (songMeta.InstrumentalAudio.IsNullOrEmpty())
            {
                errorMessage = "No instrumental audio found. Split the audio first.";
                return false;
            }

            if (!WebRequestUtils.IsHttpOrHttpsUri(songMeta.InstrumentalAudio)
                && !File.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.InstrumentalAudio)))
            {
                errorMessage = $"File does not exist: {songMeta.VocalsAudio}";
                return false;
            }
        }

        errorMessage = "";
        return true;
    }
}
