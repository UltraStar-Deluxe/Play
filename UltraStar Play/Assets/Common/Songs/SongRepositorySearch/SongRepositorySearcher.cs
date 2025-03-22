using System;
using System.Collections.Generic;
using UnityEngine;

public static class SongRepositorySearcher
{
    public static async Awaitable<List<SongRepositorySearchResult>> SearchSongsAsync(SongRepositorySearchParameters searchParameters)
    {
        List<SongRepositorySearchResult> result = new();

        List<ISongRepository> songRepositories = ModManager.GetModObjects<ISongRepository>();
        foreach (ISongRepository songRepository in songRepositories)
        {
            try
            {
                SongRepositorySearchResult searchResultEntry = await songRepository.SearchSongsAsync(searchParameters);
                result.Add(searchResultEntry);
            }
            catch (Exception ex)
            {
                ex.Log($"Failed to search songs with {songRepository}");
            }
        }

        await Awaitable.MainThreadAsync();
        return result;
    }
}
