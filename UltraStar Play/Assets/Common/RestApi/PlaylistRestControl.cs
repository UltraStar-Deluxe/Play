using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using SimpleHttpServerForUnity;
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
        httpServer.CreateEndpoint(HttpMethod.Get, "api/rest/playlist/favorites")
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
                        Hash = songMeta.SongHash,
                    })
                    .ToList();
                requestData.Context.Response.WriteJson(songListDto);
            });

        httpServer.CreateEndpoint(HttpMethod.Post, "api/rest/playlist/favorites/entry/{songId}")
            .SetDescription($"Add song to the favorites playlist")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];
                SongMeta songMeta = songMetaManager.GetSongMetaById(songId);
                playlistManager.AddSongToPlaylist(playlistManager.FavoritesPlaylist, songMeta);
            });

        httpServer.CreateEndpoint(HttpMethod.Delete, "api/rest/playlist/favorites/entry/{songId}")
            .SetDescription($"Remove song from the favorites playlist")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];
                SongMeta songMeta = songMetaManager.GetSongMetaById(songId);
                playlistManager.RemoveSongFromPlaylist(playlistManager.FavoritesPlaylist, songMeta);
            });
	}
}
