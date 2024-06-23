using System;
using UniInject;
using UniRx;
using UnityEngine;

public abstract class AbstractAudioSupportProvider : MonoBehaviour, INeedInjection, IAudioSupportProvider
{
    [Inject]
    protected Settings settings;

    [Inject]
    protected SceneNavigator sceneNavigator;

    public abstract IObservable<AudioLoadedEvent> LoadAsObservable(string audioUri, bool streamAudio, double startPositionInMillis);
    public abstract bool IsSupported(string audioUri);
    public abstract void Unload();
    public abstract void Play();
    public abstract void Pause();
    public abstract void Stop();
    public abstract bool IsPlaying { get; set; }
    public abstract double PlaybackSpeed { get; set; }
    public abstract void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch);
    public abstract double PositionInMillis { get; set; }
    public abstract double DurationInMillis { get; }
    public abstract double VolumeFactor { get; set; }

    protected virtual void OnDestroy()
    {
        Unload();
    }

    protected virtual void OnInjectionFinished()
    {
        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(evt =>
            {
                Stop();
                Unload();
            })
            .AddTo(gameObject);
    }
}
