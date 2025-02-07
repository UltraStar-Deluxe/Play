using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

public class MidiAudioSupportProvider : AbstractAudioSupportProvider
{
    [InjectedInInspector]
    public AudioSourceAudioSupportProvider audioSourceAudioSupportProvider;

    [Inject]
    private MidiManager midiManager;

    public override async Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        AudioClip audioClip = midiManager.CreateAudioClip(audioUri);
        if (audioClip == null)
        {
            throw new SongAudioPlayerException($"Failed to load audio clip from MIDI file {audioUri}");
        }

        audioSourceAudioSupportProvider.audioSource.clip = audioClip;
        PositionInMillis = startPositionInMillis;
        Play();
        return new AudioLoadedEvent(audioUri);
    }

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
               && ApplicationUtils.IsSupportedMidiFormat(Path.GetExtension(audioUri));
    }

    public override void Unload()
    {
        midiManager.StopAllMidiNotes();
        audioSourceAudioSupportProvider.Unload();
    }

    public override void Play()
    {
        audioSourceAudioSupportProvider.Play();
    }

    public override void Pause()
    {
        audioSourceAudioSupportProvider.Pause();
    }

    public override void Stop()
    {
        audioSourceAudioSupportProvider.Stop();
    }

    public override bool IsPlaying
    {
        get => audioSourceAudioSupportProvider.IsPlaying;
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
        get => 1;
        set => SetPlaybackSpeed(value, true);
    }

    public override void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch)
    {
        audioSourceAudioSupportProvider.SetPlaybackSpeed(newValue, changeTempoButKeepPitch);
    }

    public override double PositionInMillis
    {
        get => audioSourceAudioSupportProvider.PositionInMillis;
        set => audioSourceAudioSupportProvider.PositionInMillis = value;
    }

    public override double DurationInMillis => audioSourceAudioSupportProvider.DurationInMillis;

    public override double VolumeFactor
    {
        get => audioSourceAudioSupportProvider.VolumeFactor;
        set => audioSourceAudioSupportProvider.VolumeFactor = value;
    }
}
