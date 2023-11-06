using System;
using System.Collections.Generic;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class CurrentOnlineGameUiControl : INeedInjection, IInjectionFinishedListener, IDisposable
    {
        [Inject(UxmlName = R.UxmlNames.connectedClientsListTitle)]
        private Label connectedClientsListTitle;

        [Inject(UxmlName = R.UxmlNames.connectedClientsListScrollView)]
        private VisualElement connectedClientsListScrollView;

        [Inject(UxmlName = R.UxmlNames.disconnectOnlineGameButton)]
        private Button disconnectOnlineGameButton;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private SteamMultiplayerManager steamMultiplayerManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        private readonly List<IDisposable> disposables = new();

        public void OnInjectionFinished()
        {
            disconnectOnlineGameButton.RegisterCallbackButtonTriggered(_ => networkManager.Shutdown());

            // TODO: Get lobby name from server and display it.
            connectedClientsListTitle.text = "Connected Players";

            // TODO: Get connected clients from host and display them.
            connectedClientsListScrollView.Clear();

            steamLobbyManager
                .LobbyEventStream
                .Subscribe(evt =>
                {
                    if (evt is LobbyCreatedEvent
                            or MemberJoinedLobbyEvent
                            or MemberLeftLobbyEvent
                            or LobbyDataChangedEvent)
                    {
                        OnSteamLobbyChanged();
                    }
                });
        }

        private void OnSteamLobbyChanged()
        {
            Lobby? lobby = steamLobbyManager.CurrentLobby;

            if (lobby.HasValue)
            {
                connectedClientsListTitle.text = $"Members of \"{lobby?.GetName()}\"";

                connectedClientsListScrollView.Clear();
                lobby?.Members.ForEach(friend => connectedClientsListScrollView.Add(new Label(friend.Name)));
            }
            else
            {
                connectedClientsListTitle.text = "Not connected";
                connectedClientsListScrollView.Clear();
            }
        }

        public void Dispose()
        {
            disposables.ForEach(it => it.Dispose());
        }
    }
}
