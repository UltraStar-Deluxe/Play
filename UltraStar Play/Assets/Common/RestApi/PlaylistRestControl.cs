using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlaylistRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private SongMetaManager songMetaManager;
    
    [Inject]
    private PlaylistManager playlistManager;

    private void Start()
    {
        httpServer.On(HttpMethod.Get, "api/rest/playlist/favorites")
            .WithDescription($"Get songs of the 'favorites' playlist")
            .UntilDestroy(gameObject)
            .Do(requestData =>
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

        httpServer.On(HttpMethod.Post, "api/rest/playlist/favorites/entry/{songId}")
            .WithDescription($"Add song to the favorites playlist")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string songId = requestData.PathParameters["songId"];
                SongMeta songMeta = songMetaManager.GetSongMetaById(songId);
                playlistManager.AddSongToPlaylist(playlistManager.FavoritesPlaylist, songMeta);
            });

        httpServer.On(HttpMethod.Delete, "api/rest/playlist/favorites/entry/{songId}")
            .WithDescription($"Remove song from the favorites playlist")
            .UntilDestroy(gameObject)
            .Do(requestData =>
            {
                string songId = requestData.PathParameters["songId"];
                SongMeta songMeta = songMetaManager.GetSongMetaById(songId);
                playlistManager.RemoveSongFromPlaylist(playlistManager.FavoritesPlaylist, songMeta);
            });
	}
}
