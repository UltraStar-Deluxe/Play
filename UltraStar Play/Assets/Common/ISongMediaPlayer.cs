using System;

public interface ISongMediaPlayer<T> where T : ISongMediaLoadedEvent
{
    public double PositionInMillis { get; }
    public double DurationInMillis { get; }
    public IObservable<T> LoadAndPlayAsObservable(SongMeta songMeta);
}
