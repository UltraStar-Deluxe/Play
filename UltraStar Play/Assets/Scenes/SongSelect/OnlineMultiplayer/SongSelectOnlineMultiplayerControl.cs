using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;

public class SongSelectOnlineMultiplayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private SongSelectSceneData sceneData;

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

        if (onlineMultiplayerManager.IsHost)
        {
            // Check all players have song before starting it
            disposables.Add(songSelectSceneControl.BeforeSongStartedEventStream.Subscribe(evt =>
            {
                evt.CancelReason = "need to check that all players have song locally";
                AllPlayersHaveSongLocallyAsObservable(evt.SongMeta)
                    .CatchIgnore((Exception ex) =>
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"Failed to get whether all players have song locally: {ex.Message}");
                        NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
                    })
                    .Subscribe(result =>
                    {
                        if (result.AllPlayersHaveSongLocally)
                        {
                            songSelectSceneControl.StartSingSceneWithGivenSongAndSettings(evt.SongMeta, true, false);
                        }
                        else
                        {
                            NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_error_playersDontHaveSongLocally,
                                "names", result.PlayersThatDoNotHaveSongLocally.JoinWith(", ")));
                        }
                    });
            }));

            // Start singing also for non-host player
            disposables.Add(sceneNavigator.BeforeSceneChangeEventStream.Subscribe(evt =>
            {
                if (evt.NextScene is not EScene.SingScene)
                {
                    return;
                }

                SingSceneData singSceneData = evt.SceneData as SingSceneData;
                SingSceneDataDto singSceneDataDto = NetcodeMessageDtoConverterUtils.ToDto(singSceneData);
                onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
                    nameof(StartSingSceneRequestDto),
                    FastBufferWriterUtils.WriteJsonValuePacked(new StartSingSceneRequestDto()
                    {
                        SingSceneDataDto = singSceneDataDto,
                    }),
                    onlineMultiplayerManager.AllLobbyMembersUnityNetcodeClientIds);
            }));
        }
        else
        {
            // Cannot start song directly, but suggest it to host.
            disposables.Add(songSelectSceneControl.BeforeSongStartedEventStream.Subscribe(evt =>
            {
                evt.CancelReason = "only host can start singing";
                ShowSuggestSongToHostDialog(evt.SongMeta);
            }));
        }

        // Handle song suggestion
        disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
            nameof(SuggestSongRequestDto),
            request =>
            {
                SuggestSongRequestDto requestDto = FastBufferReaderUtils.ReadJsonValuePacked<SuggestSongRequestDto>(request.MessagePayload);
                LobbyMember lobbyMember = onlineMultiplayerManager.LobbyMemberManager.GetLobbyMember(request.SenderNetcodeClientId);
                SongMeta songMeta = songMetaManager.GetSongMetaByGloballyUniqueId(requestDto.GloballyUniqueSongId);
                if (songMeta != null
                    && songMeta != songSelectSceneControl.SelectedSong)
                {
                    uiManager.CreateConfirmationDialogControl(
                        Translation.Get(R.Messages.songSelectScene_receivedSongSuggestionDialog_title),
                        Translation.Get(R.Messages.songSelectScene_receivedSongSuggestionDialog_message,
                            "suggestionName", songMeta.GetArtistDashTitle(),
                            "suggestorName", lobbyMember.DisplayName),
                        Translation.Get(R.Messages.common_yes),
                        _ => songRouletteControl.SelectEntryBySongMeta(songMeta),
                        Translation.Get(R.Messages.common_no));
                }
            }));
    }

    private void ShowSuggestSongToHostDialog(SongMeta songMeta)
    {
        uiManager.CreateConfirmationDialogControl(
            Translation.Get(R.Messages.songSelectScene_sendSongSuggestionDialog_title),
            Translation.Get(R.Messages.songSelectScene_sendSongSuggestionDialog_message,
                "suggestionName", songMeta.GetArtistDashTitle()),
            Translation.Get(R.Messages.common_yes),
            _ => SendSuggestSongMessage(songMeta),
            Translation.Get(R.Messages.common_no));
    }

    private void SendSuggestSongMessage(SongMeta songMeta)
    {
        onlineMultiplayerManager.MessagingControl.SendNamedMessageToClient(
            nameof(SuggestSongRequestDto),
            FastBufferWriterUtils.WriteJsonValuePacked(new SuggestSongRequestDto(SongIdManager.GetAndCacheGloballyUniqueId(songMeta))),
            NetworkManager.ServerClientId);
        NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_suggestedSongToHost,
            "name", songMeta.GetArtistDashTitle()));
    }

    public IObservable<AllPlayersHaveSongLocallyResult> AllPlayersHaveSongLocallyAsObservable(SongMeta songMeta)
    {
        return Observable.Create<AllPlayersHaveSongLocallyResult>(o =>
        {
            AllPlayersHaveSongLocallyResult result = new();

            HasSongRequestDto requestDto = new HasSongRequestDto(SongIdManager.GetAndCacheGloballyUniqueId(songMeta));
            onlineMultiplayerManager.ObservableMessagingControl.SendNamedMessageToClientsAsObservable(
                    nameof(HasSongRequestDto),
                    FastBufferWriterUtils.WriteJsonValuePacked(requestDto),
                    onlineMultiplayerManager.AllLobbyMembersUnityNetcodeClientIds)
                .CatchIgnore((Exception ex) =>
                {
                    o.OnError(ex);
                })
                .DoOnCompleted(() =>
                {
                    o.OnNext(result);
                    o.OnCompleted();
                })
                .Subscribe(response =>
                {
                    HasSongResponseDto responseDtoDto = FastBufferReaderUtils.ReadJsonValuePacked<HasSongResponseDto>(response.MessagePayload);
                    if (responseDtoDto.HasSong)
                    {
                        return;
                    }

                    LobbyMemberPlayerProfile lobbyMemberPlayerProfile = nonPersistentSettings.LobbyMemberPlayerProfiles
                        .FirstOrDefault(it => it.UnityNetcodeClientId == response.SenderNetcodeClientId);
                    result.AddPlayerThatDoesNotHaveSongLocally(lobbyMemberPlayerProfile?.Name);
                });

            return Disposable.Empty;
        });
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
    }

    public class AllPlayersHaveSongLocallyResult
    {
        public bool AllPlayersHaveSongLocally { get; private set; }
        public List<string> PlayersThatDoNotHaveSongLocally { get; private set; } = new();

        public AllPlayersHaveSongLocallyResult()
        {
            AllPlayersHaveSongLocally = true;
        }

        public void AddPlayerThatDoesNotHaveSongLocally(string playerName)
        {
            AllPlayersHaveSongLocally = false;
            PlayersThatDoNotHaveSongLocally.Add(playerName);
        }
    }
}
