using UnityEngine;

public interface IAudioSupportProvider
{
    public Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis);
    public bool IsSupported(string audioUri);
    public void Unload();
    public void Play();
    public void Pause();
    public void Stop();
    public bool IsPlaying { get; set; }
    public double PlaybackSpeed { get; set; }
    public void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch);
    public double PositionInMillis { get; set; }
    public double DurationInMillis { get; }
    public double VolumeFactor { get; set; }
}
