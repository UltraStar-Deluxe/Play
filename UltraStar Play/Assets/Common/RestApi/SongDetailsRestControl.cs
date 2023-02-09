using System;
using System.Collections.Generic;
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
    public static SongDetailsRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongDetailsRestControl>();

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
        httpServer.CreateEndpoint(HttpMethod.Get, "api/rest/song/{songId}")
            .SetDescription($"Get song details.")
            .SetRemoveOnDestroy(gameObject)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];

                SongMeta songMeta = songMetaManager.GetSongMetaById(songId);
                if (songMeta == null)
                {
                    Debug.Log($"Cannot return song details. No song found with id {songId}.");
                    requestData.Context.Response.WriteJson(new ErrorMessageDto("Song not found"));
                }

                bool isFavorite = playlistManager.HasSongEntry(playlistManager.FavoritesPlaylist, songMeta);
                
                SongDetailsDto songDetailsDto = new()
                {
                    SongId = songId,
                    IsFavorite = isFavorite,
                    VoiceNameToLyricsMap = CreateVoiceNameToLyricsMap(songMeta),
                };

                Debug.Log($"Returning song details for song {songId}");
                requestData.Context.Response.WriteJson(songDetailsDto);
            });
        
        httpServer.CreateEndpoint(HttpMethod.Get, "api/rest/songImage/{songId}")
            .SetDescription($"Get song cover image. Returns the background image if no cover image was found.")
            .SetRemoveOnDestroy(gameObject)
            .SetThread(ResponseThread.NewThread)
            .SetCallbackAndAdd(requestData =>
            {
                string songId = requestData.PathParameters["songId"];

                SongMeta songMeta = songMetaManager.GetSongMetaById(songId);
                if (songMeta == null)
                {
                    Debug.Log($"Cannot return song image. No song found with id {songId}.");
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

                bool sendResponseComplete = false;
                MainThreadDispatcher.Send(state =>
                {
                    ImageManager.LoadSpriteFromUri(imageUri, loadedSprite =>
                        {
                            byte[] jpgBytes = loadedSprite.texture.EncodeToJPG();
                            string jpgBytesBase64 = Convert.ToBase64String(jpgBytes);
                            ImageDto imageDto = new()
                            {
                                JpgBytesBase64 = jpgBytesBase64,
                            };
                            
                            Debug.Log($"Returning song image for song {songId}");
                            requestData.Context.Response.WriteJson(imageDto);
                            sendResponseComplete = true;
                        },
                        failedUnityWebRequest =>
                        {
                            requestData.Context.Response.WriteJson(new ErrorMessageDto("Failed to load song image"));
                            sendResponseComplete = true;
                            Debug.LogError($"Failed to load song image from uri {imageUri}");
                        });
                }, null);
                
                // Wait until the coroutine is finished.
                // Otherwise the response is sent before the image is loaded.
                Debug.Log($"Waiting for load image to complete");
                long maxWaitTimeInMillis = 5000;
                long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
                while(!sendResponseComplete)
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

    private Dictionary<string,string> CreateVoiceNameToLyricsMap(SongMeta songMeta)
    {
        Dictionary<string, string> voiceNameToLyricsMap = new();
        foreach (Voice voice in songMeta.GetVoices())
        {
            string voiceDisplayName = songMeta.VoiceNames[voice.Name];
            voiceNameToLyricsMap.Add(voiceDisplayName, SongMetaUtils.GetLyrics(voice, true));
        }
        return voiceNameToLyricsMap;
    }
}
