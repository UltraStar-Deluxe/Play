using System;
using UnityEngine;

public interface IVideoSupportProvider
{
    public IObservable<VideoLoadedEvent> LoadVideoAsObservable(string videoUri);
    public void UnloadVideo();
    public void PlayVideo();
    public void PauseVideo();
    public void StopVideo();
    public void SetBackgroundScaleMode(ESongBackgroundScaleMode mode);
    public void SetTargetTexture(RenderTexture renderTexture);
    public bool IsPlaying { get; set; }
    public bool IsLooping { get; set; }
    public float PlaybackSpeed { get; set; }
    public double PositionInVideoInMillis { get; set; }
    public double DurationInMillis { get; }
    public EVideoSupportProvider VideoSupportProvider { get; }
}
