using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

public class AudioSourceAudioSupportProvider : AbstractAudioSupportProvider
{
    [InjectedInInspector]
    public AudioSource audioSource;

    public override async Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        AudioClip loadedAudioClip = await AudioManager.LoadAudioClipFromUriAsync(audioUri, streamAudio);
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load audio clip '{audioUri}': {nameof(AudioSourceAudioSupportProvider)} has been destroyed already.");
        }

        if (loadedAudioClip == null)
        {
            audioSource.Stop();
            string errorMessage = $"Failed to load audio clip from {audioUri}";
            Debug.LogError(errorMessage);
            throw new AudioSupportProviderException(errorMessage);
        }

        audioSource.clip = loadedAudioClip;
        PositionInMillis = startPositionInMillis;
        return new AudioLoadedEvent(audioUri);
    }

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
            && settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(audioUri));
    }

    public override void Unload()
    {
        PitchShifterUtils.ResetPitchAndPitchShifter(audioSource);
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
            PitchShifterUtils.SetPitchWithPitchShifter(audioSource, (float)newValue);
        }
        else
        {
            audioSource.pitch = (float)newValue;
        }
    }

    public override double PositionInMillis
    {
        // Must use audioSource.timeSamples
        // because audioSource.time is not always updated by Unity when AudioSource is paused.
        get
        {
            double samplesPerMillisecond = SamplesPerMillisecond;
            if (samplesPerMillisecond <= 0)
            {
                return 0;
            }
            return audioSource.timeSamples / samplesPerMillisecond;
        }
        set
        {
            double samplesPerMillisecond = SamplesPerMillisecond;
            if (samplesPerMillisecond <= 0)
            {
                return;
            }
            audioSource.timeSamples = (int)(value * samplesPerMillisecond);
        }
    }

    public override double DurationInMillis => audioSource.clip.length * 1000.0;

    public override double VolumeFactor
    {
        get => audioSource.volume;
        set => audioSource.volume = (float)value;
    }

    private double SamplesPerMillisecond => audioSource.clip.frequency / 1000.0;
}
