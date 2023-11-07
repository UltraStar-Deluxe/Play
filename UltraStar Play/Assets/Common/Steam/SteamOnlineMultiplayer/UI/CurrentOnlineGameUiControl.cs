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
    public class CurrentOnlineGameUiControl : INeedInjection, IInjectionFinishedListener, IDisposable
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

        private readonly List<NetworkClientEntryControl> entryControls = new();

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
                    if (evt is LobbyCreatedEvent
                            or MemberJoinedLobbyEvent
                            or MemberLeftLobbyEvent
                            or LobbyDataChangedEvent)
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

                NetworkPlayerControl networkPlayerControl = localPlayerObject.GetComponent<NetworkPlayerControl>();
                if (networkPlayerControl == null)
                {
                    Debug.LogError("Missing NetworkPlayerControl");
                    return;
                }

                networkPlayerControl.SendRequestToServerAsObservable(new ConnectedMemberDatasRequestDto().ToJson())
                    .Subscribe(response =>
                    {
                        ConnectedMemberDatasResponseDto responseDto = JsonConverter.FromJson<ConnectedMemberDatasResponseDto>(response);
                        FillConnectedClientList(responseDto.MemberDatas);
                    });
            }
        }

        private void FillConnectedClientList(List<MemberData> memberDatas)
        {
            foreach (MemberData memberData in memberDatas)
            {
                CreateConnectedClientEntryControl(memberData);
            }

            ThemeManager.ApplyThemeSpecificStylesToVisualElements(connectedClientsListScrollView);
        }

        private void CreateConnectedClientEntryControl(MemberData memberData)
        {
            VisualElement visualElement = connectedClientEntryUi.CloneTreeAndGetFirstChild();
            connectedClientsListScrollView.Add(visualElement);

            NetworkClientEntryControl entryControl = injector
                .WithBindingForInstance(memberData)
                .WithRootVisualElement(visualElement)
                .CreateAndInject<NetworkClientEntryControl>();

            entryControls.Add(entryControl);
        }

        public void Dispose()
        {
            disposables.ForEach(it => it.Dispose());
        }
    }
}
