using UnityEngine;

public interface ISongMediaPlayer<T> where T : ISongMediaLoadedEvent
{
    public double PositionInMillis { get; }
    public double DurationInMillis { get; }
    public Awaitable<T> LoadAndPlayAsync(SongMeta songMeta);
}
