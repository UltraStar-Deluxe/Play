public interface IMediaSupportProvider
{
    public void Unload();
    public void Play();
    public void Pause();
    public void Stop();
    public bool IsPlaying { get; set; }
    public double PlaybackSpeed { get; set; }
    public double PositionInMillis { get; set; }
    public double DurationInMillis { get; }
}
