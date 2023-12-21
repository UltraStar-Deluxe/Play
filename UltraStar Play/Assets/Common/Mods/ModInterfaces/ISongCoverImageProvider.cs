using System;

public interface ISongCoverImageProvider : IMod
{
    public IObservable<string> GetCoverImageUri(SongMeta songMeta);
}
