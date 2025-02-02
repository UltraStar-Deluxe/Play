using System.Collections.Generic;
using UnityEngine;

public interface ISongRepository : IMod
{
    public Awaitable<List<SongRepositorySearchResultEntry>> SearchSongsAsync(SongRepositorySearchParameters searchParameters);
}
