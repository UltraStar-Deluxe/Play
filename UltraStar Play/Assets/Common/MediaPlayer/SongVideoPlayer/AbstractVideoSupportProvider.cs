using UniInject;
using UniRx;
using UnityEngine;

public abstract class AbstractVideoSupportProvider : MonoBehaviour, INeedInjection, IVideoSupportProvider
{
    [Inject]
    protected Settings settings;

    [Inject]
    protected SceneNavigator sceneNavigator;

    public abstract Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis);
    public abstract bool IsSupported(string videoUri, bool videoEqualsAudio);
    public abstract void Unload();
    public abstract void Play();
    public abstract void Pause();
    public abstract void Stop();
    public abstract void SetTargetTexture(RenderTexture renderTexture);
    public abstract bool IsPlaying { get; set; }
    public abstract bool IsLooping { get; set; }
    public abstract double PlaybackSpeed { get; set; }
    public abstract double PositionInMillis { get; set; }
    public abstract double DurationInMillis { get; }

    protected bool IsFullyLoaded => DurationInMillis > 0;

    public virtual void SetBackgroundScaleMode(ESongBackgroundScaleMode mode)
    {
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
