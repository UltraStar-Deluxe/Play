using System;

public interface ISongBackgroundImageProvider : IMod
{
    public IObservable<string> GetBackgroundImageUri(SongMeta songMeta);
}
