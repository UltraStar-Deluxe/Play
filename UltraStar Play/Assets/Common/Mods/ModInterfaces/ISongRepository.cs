using UnityEngine;

public interface ISongRepository : IMod
{
    public Awaitable<SongRepositorySearchResult> SearchSongsAsync(SongRepositorySearchParameters searchParameters);
}
