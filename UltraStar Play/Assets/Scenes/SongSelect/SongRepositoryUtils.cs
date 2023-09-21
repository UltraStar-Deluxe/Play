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

    public static IObservable<SongMeta> SearchSongs(SongSearchParameters searchParameters)
    {
        return GetSongRepositories()
            .Select(songRepository =>
            {
                return songRepository.SearchSongs(searchParameters)
                    .CatchIgnore((Exception ex) =>
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"Failed to search songs with {songRepository}: {ex.Message}");
                    });
            })
            .Merge()
            .ObserveOnMainThread();
    }

    // public static IObservable<SongMeta> GetDefaultSongs()
    // {
    //     return GetSongRepositories()
    //         .Select(songRepository =>
    //         {
    //             return songRepository.GetDefaultSongs()
    //                 .CatchIgnore((Exception ex) =>
    //                 {
    //                     Debug.LogException(ex);
    //                     Debug.LogError($"Failed to search songs with {songRepository}: {ex.Message}");
    //                 });
    //         })
    //         .Merge()
    //         .ObserveOnMainThread();
    // }
    //
    // public static IObservable<SongMeta> GetRandomSongs()
    // {
    //     return GetSongRepositories()
    //         .Select(songRepository =>
    //         {
    //             return songRepository.GetRandomSongs()
    //                 .CatchIgnore((Exception ex) =>
    //                 {
    //                     Debug.LogException(ex);
    //                     Debug.LogError($"Failed to search songs with {songRepository}: {ex.Message}");
    //                 });
    //         })
    //         .Merge()
    //         .ObserveOnMainThread();
    // }
}
