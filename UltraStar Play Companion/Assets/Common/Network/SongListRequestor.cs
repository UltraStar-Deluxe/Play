using System;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongListRequestor : MonoBehaviour, INeedInjection
{
    private readonly Subject<SongListEvent> songListEventStream = new Subject<SongListEvent>();
    public IObservable<SongListEvent> SongListEventStream => songListEventStream;

    public bool SuccessfullyLoadedAllSongs { get; private set; }

    public LoadedSongsDto LoadedSongsDto { get; private set; }

    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    public async void RequestSongList()
    {
        if (!mainGameHttpClient.IsConnected)
        {
            FireErrorMessageEvent(Translation.Get(R.Messages.companionApp_songList_error_notConnected));
            return;
        }

        try
        {
            string response = await mainGameHttpClient.GetRequestAsync(RestApiEndpointPaths.Songs);
            HandleSongListResponse(response);
        }
        catch (Exception e)
        {
            HandleSongListErrorResponse(e);
        }
    }

    private void HandleSongListResponse(string response)
    {
        try
        {
            LoadedSongsDto = JsonConverter.FromJson<LoadedSongsDto>(response);
            if (!LoadedSongsDto.IsSongScanFinished
                && LoadedSongsDto.SongCount == 0)
            {
                SuccessfullyLoadedAllSongs = false;
                FireErrorMessageEvent(Translation.Get(R.Messages.companionApp_songList_error_noSongsFound));
                return;
            }

            if (LoadedSongsDto.IsSongScanFinished)
            {
                SuccessfullyLoadedAllSongs = true;
            }

            songListEventStream.OnNext(new SongListEvent
            {
                LoadedSongsDto = LoadedSongsDto,
            });
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SuccessfullyLoadedAllSongs = false;
            FireErrorMessageEvent(Translation.Get(R.Messages.companionApp_songList_error_general));
        }
    }

    private void HandleSongListErrorResponse(Exception ex)
    {
        FireErrorMessageEvent(Translation.Get(R.Messages.companionApp_songList_error_general));
    }

    private void FireErrorMessageEvent(string errorMessage)
    {
        Debug.LogError(errorMessage);
        songListEventStream.OnNext(new SongListEvent
        {
            ErrorMessage = errorMessage,
        });
    }
}
