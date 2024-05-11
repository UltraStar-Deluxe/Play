using System;using UniInject;
using UnityEngine;

public abstract class AbstractVideoSupportProvider : MonoBehaviour, INeedInjection, IVideoSupportProvider
{
    public abstract IObservable<VideoLoadedEvent> LoadVideoAsObservable(string videoUri);
    public abstract void UnloadVideo();
    public abstract void PlayVideo();
    public abstract void PauseVideo();
    public abstract void StopVideo();
    public abstract void SetTargetTexture(RenderTexture renderTexture);
    public abstract bool IsPlaying { get; set; }
    public abstract bool IsLooping { get; set; }
    public abstract float PlaybackSpeed { get; set; }
    public abstract double PositionInVideoInMillis { get; set; }
    public abstract double DurationInMillis { get; }
    public abstract EVideoSupportProvider VideoSupportProvider { get; }

    public virtual void SetBackgroundScaleMode(ESongBackgroundScaleMode mode)
    {
    }
}
