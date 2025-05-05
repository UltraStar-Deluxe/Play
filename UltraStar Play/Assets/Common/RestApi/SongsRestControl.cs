using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongDetailsRestControl : AbstractRestControl, INeedInjection
{
    public static SongDetailsRestControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongDetailsRestControl>();

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
        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.Songs)
            .SetDescription("Get loaded songs")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(SendLoadedSongs);

        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.Song)
            .SetDescription($"Get song details.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];

                SongMeta songMeta = songMetaManager.GetSongMetaByLocallyUniqueId(songId);
                if (songMeta == null)
                {
                    Debug.Log($"Cannot return song details. No song found with locally unique id {songId}.");
                    requestData.Context.Response.WriteJson(new ErrorMessageDto("Song not found"));
                }

                bool isFavorite = playlistManager.HasSongEntry(playlistManager.FavoritesPlaylist, songMeta);

                SongDetailsDto songDetailsDto = new()
                {
                    SongId = songId,
                    IsFavorite = isFavorite,
                    VoiceDisplayNameToLyricsMap = CreateVoiceDisplayNameToLyricsMap(songMeta),
                };

                Debug.Log($"Returning song details for song {songId}");
                requestData.Context.Response.WriteJson(songDetailsDto);
            });

        httpServer.CreateEndpoint(HttpMethod.Get, RestApiEndpointPaths.SongImage)
            .SetDescription($"Get song cover image. Returns the background image if no cover image was found.")
            .SetRemoveOnDestroy(gameObject)
            .SetThread(ResponseThread.NewThread)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];

                SongMeta songMeta = songMetaManager.GetSongMetaByLocallyUniqueId(songId);
                if (songMeta == null)
                {
                    Debug.Log($"Cannot return song image. No song found with locally unique id {songId}.");
                    requestData.Context.Response.WriteJson(new ErrorMessageDto("Song not found"));
                }

                string imageUri = SongMetaUtils.GetCoverUri(songMeta);
                if (imageUri.IsNullOrEmpty())
                {
                    // Try the background image as fallback
                    imageUri = SongMetaUtils.GetBackgroundUri(songMeta);
                    if (imageUri.IsNullOrEmpty())
                    {
                        Debug.Log($"Cannot return song image. No cover or background image set for song {songId}.");
                        ImageDto imageDto = new()
                        {
                            JpgBytesBase64 = "",
                        };
                        requestData.Context.Response.WriteJson(imageDto);
                        return;
                    }
                }

                Awaitable sendSongImageResponseAsync = SendSongImageResponseAsync(requestData, songId, imageUri);

                // Wait until the coroutine is finished.
                // Otherwise the response is sent before the image is loaded.
                Debug.Log($"Waiting for load image to complete");
                long maxWaitTimeInMillis = 5000;
                long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
                while(!sendSongImageResponseAsync.IsCompleted)
                {
                    long durationInMillis = TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis;
                    if (durationInMillis > maxWaitTimeInMillis)
                    {
                        Debug.LogWarning($"Emergency exit. Loading image did not complete withing {maxWaitTimeInMillis} ms");
                        return;
                    }
                    Thread.Sleep(100);
                }
                Debug.Log($"Load image completed after {TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis} ms");
            });
	}

    private async Awaitable SendSongImageResponseAsync(EndpointRequestData requestData, string songId, string imageUri)
    {
        try
        {
            await Awaitable.MainThreadAsync();
            Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(imageUri);

            byte[] jpgBytes = loadedSprite.texture.EncodeToJPG();
            string jpgBytesBase64 = Convert.ToBase64String(jpgBytes);
            ImageDto imageDto = new() { JpgBytesBase64 = jpgBytesBase64, };

            Debug.Log($"Returning song image for song {songId}");
            requestData.Context.Response.WriteJson(imageDto);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load song image from uri '{imageUri}': {ex.Message}");
            requestData.Context.Response.WriteJson(new ErrorMessageDto("Failed to load song image"));
        }
    }

    private Dictionary<string, string> CreateVoiceDisplayNameToLyricsMap(SongMeta songMeta)
    {
        Dictionary<string, string> voiceIdToLyricsMap = new();
        foreach (Voice voice in songMeta.Voices)
        {
            string voiceDisplayName = songMeta.GetVoiceDisplayName(voice.Id);
            voiceIdToLyricsMap.Add(voiceDisplayName, SongMetaUtils.GetLyrics(voice, true));
        }
        return voiceIdToLyricsMap;
    }

    private void SendLoadedSongs(EndpointRequestData requestData)
    {
        SongMetaManager songMetaManager = SongMetaManager.Instance;
        requestData.Context.Response.SendResponse(new LoadedSongsDto
        {
            IsSongScanFinished = songMetaManager.IsSongScanFinished,
            SongCount = songMetaManager.GetSongMetas().Count,
            SongList = songMetaManager.GetSongMetas()
                .Select(songMeta => new SongDto
                {
                    Artist = songMeta.Artist,
                    Title = songMeta.Title,
                    Hash = SongIdManager.GetAndCacheLocallyUniqueId(songMeta),
                })
                .ToList()
        }.ToJson());
    }
}
