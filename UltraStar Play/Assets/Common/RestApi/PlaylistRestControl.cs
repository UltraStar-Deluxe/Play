using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlaylistRestControl : AbstractRestControl, INeedInjection
{
    public static PlaylistRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<PlaylistRestControl>();

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private PlaylistManager playlistManager;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        httpServer.CreateEndpoint(HttpMethod.Get, HttpApiEndpointPaths.PlaylistFavorites)
            .SetDescription($"Get songs of the 'favorites' playlist")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                List<SongMeta> songMetas = playlistManager.GetSongMetas(playlistManager.FavoritesPlaylist);
                SongListDto songListDto = new SongListDto();
                songListDto.Songs = songMetas
                    .Select(songMeta => new SongDto()
                    {
                        Artist = songMeta.Artist,
                        Title = songMeta.Title,
                        Hash = SongIdManager.GetAndCacheLocallyUniqueId(songMeta),
                    })
                    .ToList();
                requestData.Context.Response.WriteJson(songListDto);
            });

        httpServer.CreateEndpoint(HttpMethod.Post, HttpApiEndpointPaths.PlaylistFavoritesEntry)
            .SetDescription($"Add song to the favorites playlist")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];
                SongMeta songMeta = songMetaManager.GetSongMetaByLocallyUniqueId(songId);
                if (songMeta == null)
                {
                    Debug.Log($"Cannot add song to favorites. No song found with locally unique id {songId}.");
                    requestData.Context.Response.WriteJson(new ErrorMessageDto("Song not found"));
                }
                playlistManager.AddSongToPlaylist(playlistManager.FavoritesPlaylist, songMeta);
            });

        httpServer.CreateEndpoint(HttpMethod.Delete, HttpApiEndpointPaths.PlaylistFavoritesEntry)
            .SetDescription($"Remove song from the favorites playlist")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];
                SongMeta songMeta = songMetaManager.GetSongMetaByLocallyUniqueId(songId);
                if (songMeta == null)
                {
                    Debug.Log($"Cannot remove song from favorites. No song found with locally unique id {songId}.");
                    requestData.Context.Response.WriteJson(new ErrorMessageDto("Song not found"));
                }
                playlistManager.RemoveSongFromPlaylist(playlistManager.FavoritesPlaylist, songMeta);
            });
	}
}
