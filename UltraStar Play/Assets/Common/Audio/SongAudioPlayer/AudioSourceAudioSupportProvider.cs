using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

public class AudioSourceAudioSupportProvider : AbstractAudioSupportProvider
{
    [InjectedInInspector]
    public AudioSource audioSource;

    public override IObservable<AudioLoadedEvent> LoadAsObservable(string audioUri, bool streamAudio)
    {
        return AudioManager.LoadAudioClipFromUri(audioUri, streamAudio)
            .Select(loadedAudioClip =>
            {
                if (this == null)
                {
                    string errorMessage = $"Failed to load audio clip '{audioUri}': {nameof(AudioSourceAudioSupportProvider)} has been destroyed already.";
                    Debug.LogError(errorMessage);
                    throw new AudioSupportProviderException(errorMessage);
                }

                if (loadedAudioClip == null)
                {
                    audioSource.Stop();
                    string errorMessage = $"Failed to load audio clip from {audioUri}";
                    Debug.LogError(errorMessage);
                    throw new AudioSupportProviderException(errorMessage);
                }

                audioSource.clip = loadedAudioClip;
                return new AudioLoadedEvent(audioUri);
            });
    }

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
            && settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(audioUri));
    }

    public override void Unload()
    {
        AudioUtils.ResetPitchAndPitchShifter(audioSource);
        audioSource.Stop();
        audioSource.clip = null;
    }

    public override void Play()
    {
        audioSource.Play();
    }

    public override void Pause()
    {
        audioSource.Pause();
    }

    public override void Stop()
    {
        audioSource.Stop();
    }

    public override bool IsPlaying
    {
        get => audioSource.isPlaying;
        set
        {
            if (value)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }
    }

    public override double PlaybackSpeed
    {
        get => audioSource. pitch;
        set => SetPlaybackSpeed(value, true);
    }

    public override void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch)
    {
        if (changeTempoButKeepPitch)
        {
            AudioUtils.SetPitchWithPitchShifter(audioSource, (float)newValue);
        }
        else
        {
            audioSource.pitch = (float)newValue;
        }
    }

    public override double PositionInMillis
    {
        get => audioSource.time * 1000.0;
        set => audioSource.time = (float)(value / 1000.0);
    }

    public override double DurationInMillis => audioSource.clip.length * 1000.0;

    public override double VolumeFactor
    {
        get => audioSource.volume;
        set => audioSource.volume = (float)value;
    }
}
