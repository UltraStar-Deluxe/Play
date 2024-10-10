using System;
using UnityEngine;

public interface IVideoSupportProvider
{
    public IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri, double startPositionInMillis);
    public void Unload();
    public bool IsSupported(string videoUri, bool videoEqualsAudio);
    public void Play();
    public void Pause();
    public void Stop();
    public void SetBackgroundScaleMode(ESongBackgroundScaleMode mode);
    public void SetTargetTexture(RenderTexture renderTexture);
    public bool IsPlaying { get; set; }
    public bool IsLooping { get; set; }
    public double PlaybackSpeed { get; set; }
    public double PositionInMillis { get; set; }
    public double DurationInMillis { get; }
}
