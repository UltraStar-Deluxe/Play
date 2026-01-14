using UnityEngine;

public interface IVideoSupportProvider : IMediaSupportProvider
{
    public Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis);
    public bool IsSupported(string videoUri, bool videoEqualsAudio);
    public void SetBackgroundScaleMode(ESongBackgroundScaleMode mode);
    public void SetTargetTexture(RenderTexture renderTexture);
    public bool IsLooping { get; set; }
}
