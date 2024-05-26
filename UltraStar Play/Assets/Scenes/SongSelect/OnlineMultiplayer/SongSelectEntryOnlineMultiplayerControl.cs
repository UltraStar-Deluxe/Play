using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;

public class SongSelectEntryOnlineMultiplayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    private readonly List<IDisposable> disposables = new();

    public void OnInjectionFinished()
    {
        if (!onlineMultiplayerManager.IsOnlineGame
            || onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds.IsNullOrEmpty())
        {
            return;
        }

        disposables.Add(songRouletteControl.CreatedSongSelectEntryControlEventStream
            .Subscribe(songSelectEntryControl =>
            {
                disposables.Add(songSelectEntryControl.SongSelectEntryAsObservable.Subscribe(songSelectEntry =>
                {
                    if (songSelectEntry is SongSelectSongEntry songEntry
                        && songEntry.SongMeta != null)
                    {
                        CheckOtherPlayersHaveSongLocally(songSelectEntryControl, songEntry.SongMeta);
                    }
                }));
            }));
    }

    public void CheckOtherPlayersHaveSongLocally(SongSelectEntryControl songSelectEntryControl, SongMeta songMeta)
    {
        if (onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds.IsNullOrEmpty())
        {
            return;
        }

        disposables.Add(onlineMultiplayerManager.ObservableMessagingControl.SendNamedMessageToClientsAsObservable(
                nameof(HasSongRequestDto),
                FastBufferWriterUtils.WriteJsonValuePacked(new HasSongRequestDto(SongIdManager.GetAndCacheGloballyUniqueId(songMeta))),
                onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to check whether other lobby members have song locally: song: '{songMeta.GetArtistDashTitle()}', error: {ex.Message}");
            })
            .Subscribe(response =>
            {
                if (songSelectEntryControl.SongSelectEntry is not SongSelectSongEntry songEntry
                    || songEntry.SongMeta != songMeta)
                {
                    return;
                }

                HasSongResponseDto responseDto = FastBufferReaderUtils.ReadJsonValuePacked<HasSongResponseDto>(response.MessagePayload);
                if (!responseDto.HasSong)
                {
                    Debug.Log($"Netcode client {response.SenderNetcodeClientId} does not have the song '{songMeta.GetArtistDashTitle()}', showing corresponding icon.");
                    songSelectEntryControl.ShowNotAvailableInOnlineGameIcon();
                }
            }));
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
    }
}
