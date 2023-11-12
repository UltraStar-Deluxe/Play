using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class CurrentLobbyUiControl : INeedInjection, IInjectionFinishedListener, IDisposable
    {
        [Inject(UxmlName = R.UxmlNames.connectedClientsListTitle)]
        private Label connectedClientsListTitle;

        [Inject(UxmlName = R.UxmlNames.connectedClientsListScrollView)]
        private VisualElement connectedClientsListScrollView;

        [Inject(UxmlName = R.UxmlNames.disconnectOnlineGameButton)]
        private Button disconnectOnlineGameButton;

        [Inject(Key = nameof(connectedClientEntryUi))]
        private VisualTreeAsset connectedClientEntryUi;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private SteamMultiplayerManager steamMultiplayerManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        [Inject]
        private Injector injector;

        private readonly List<IDisposable> disposables = new();

        private readonly List<LobbyMemberUiControl> entryControls = new();

        public void OnInjectionFinished()
        {
            connectedClientsListTitle.text = "Connected Players";
            connectedClientsListScrollView.Clear();

            disconnectOnlineGameButton.RegisterCallbackButtonTriggered(_ => networkManager.Shutdown());

            disposables.Add(steamMultiplayerManager
                .NetworkClientConnectionChangedEventSteam
                .Subscribe(_ => OnConnectedClientsChanged()));

            disposables.Add(steamLobbyManager
                .LobbyEventStream
                .Subscribe(evt =>
                {
                    if (evt is SteamLobbyCreatedEvent
                            or MemberJoinedSteamLobbyEvent
                            or MemberLeftSteamLobbyEvent
                            or SteamLobbyDataChangedEvent)
                    {
                        OnSteamLobbyChanged();
                    }
                }));

            UpdateConnectedClientList();
        }

        private void OnConnectedClientsChanged()
        {
            UpdateConnectedClientList();
        }

        private void OnSteamLobbyChanged()
        {
            Lobby? lobby = steamLobbyManager.CurrentLobby;

            if (lobby.HasValue)
            {
                connectedClientsListTitle.text = $"Members of \"{lobby?.GetName()}\"";
                UpdateConnectedClientList();
            }
            else
            {
                connectedClientsListTitle.text = "Not connected";
                UpdateConnectedClientList();
            }
        }

        private void UpdateConnectedClientList()
        {
            connectedClientsListScrollView.Clear();
            entryControls.Clear();

            if (networkManager.IsServer)
            {
                FillConnectedClientList(steamMultiplayerManager.GetMembers().ToList());
            }
            else if (networkManager.IsClient)
            {
                NetworkObject localPlayerObject = networkManager.SpawnManager.GetLocalPlayerObject();
                if (localPlayerObject == null)
                {
                    Debug.LogError("Missing LocalPlayerObject");
                    return;
                }

                LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = localPlayerObject.GetComponent<LobbyMemberNetworkBehaviour>();
                if (lobbyMemberNetworkBehaviour == null)
                {
                    Debug.LogError("Missing NetworkPlayerControl");
                    return;
                }

                lobbyMemberNetworkBehaviour.MessagingNetworkBehaviour.SendRequestToServerAsObservable(new CurrentLobbyMembersRequestDto().ToJson())
                    .Subscribe(response =>
                    {
                        CurrentSteamLobbyMembersResponseDto responseDto = JsonConverter.FromJson<CurrentSteamLobbyMembersResponseDto>(response);
                        FillConnectedClientList(responseDto.SteamLobbyMembers);
                    });
            }
        }

        private void FillConnectedClientList(List<SteamLobbyMember> memberDatas)
        {
            foreach (SteamLobbyMember memberData in memberDatas)
            {
                CreateConnectedClientEntryControl(memberData);
            }

            ThemeManager.ApplyThemeSpecificStylesToVisualElements(connectedClientsListScrollView);
        }

        private void CreateConnectedClientEntryControl(SteamLobbyMember steamLobbyMember)
        {
            VisualElement visualElement = connectedClientEntryUi.CloneTreeAndGetFirstChild();
            connectedClientsListScrollView.Add(visualElement);

            LobbyMemberUiControl entryControl = injector
                .WithBindingForInstance(steamLobbyMember)
                .WithRootVisualElement(visualElement)
                .CreateAndInject<LobbyMemberUiControl>();

            entryControls.Add(entryControl);
        }

        public void Dispose()
        {
            disposables.ForEach(it => it.Dispose());
        }
    }
}
