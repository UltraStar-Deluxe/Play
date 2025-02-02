using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public static class SongRepositoryUtils
{
    public static List<ISongRepository> GetSongRepositories()
    {
        return ModManager.GetModObjects<ISongRepository>();
    }

    public static async Awaitable<List<SongRepositorySearchResultEntry>> SearchSongs(SongRepositorySearchParameters searchParameters)
    {
        List<SongRepositorySearchResultEntry> result = new();

        List<ISongRepository> songRepositories = GetSongRepositories();
        foreach (ISongRepository songRepository in songRepositories)
        {
            try
            {
                List<SongRepositorySearchResultEntry> searchResultEntry = await songRepository.SearchSongsAsync(searchParameters);
                result.AddRange(searchResultEntry);
            }
            catch (Exception ex)
            {
                ExceptionUtils.LogExceptionAndError($"Failed to search songs with {songRepository}", ex);
            }
        }

        return result;
    }
}
