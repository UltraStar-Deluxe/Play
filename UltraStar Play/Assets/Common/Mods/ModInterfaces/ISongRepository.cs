using System;

public interface ISongRepository : IMod
{
    public IObservable<SongMeta> SearchSongs(SongSearchParameters searchParameters);
    // public IObservable<SongMeta> GetDefaultSongs();
    // public IObservable<SongMeta> GetRandomSongs();
}
