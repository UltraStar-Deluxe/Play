using UnityEngine;

public interface IAudioSupportProvider : IMediaSupportProvider
{
    public Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis);
    public void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch);
    public double VolumeFactor { get; set; }
}
