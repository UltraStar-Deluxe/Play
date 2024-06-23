using System;

public interface ISongRepository : IMod
{
    public IObservable<SongRepositorySearchResultEntry> SearchSongs(SongRepositorySearchParameters searchParameters);
}
