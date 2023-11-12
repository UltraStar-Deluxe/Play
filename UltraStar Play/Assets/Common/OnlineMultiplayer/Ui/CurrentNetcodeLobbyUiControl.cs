using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace CommonOnlineMultiplayer
{
    public class CurrentNetcodeLobbyUiControl : INeedInjection, IInjectionFinishedListener, ICurrentLobbyUiControl
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
        private OnlineMultiplayerManager onlineMultiplayerManager;

        [Inject]
        private NetcodeLobbyMemberManager lobbyMemberManager;

        [Inject]
        private NetcodeLobbyManager lobbyManager;

        [Inject]
        private Injector injector;

        private readonly List<IDisposable> disposables = new();

        private readonly List<LobbyMemberUiControl> entryControls = new();

        public void OnInjectionFinished()
        {
            connectedClientsListTitle.text = "Connected Players";
            connectedClientsListScrollView.Clear();

            disconnectOnlineGameButton.RegisterCallbackButtonTriggered(_ => networkManager.Shutdown());

            disposables.Add(onlineMultiplayerManager
                .LobbyMemberConnectionChangedEventSteam
                .Subscribe(_ => OnLobbyMembersChanged()));

            disposables.Add(onlineMultiplayerManager
                .LobbyMemberConnectionChangedEventSteam
                .Subscribe(evt =>
                {
                    OnLobbyMemberConnectionChanged();
                }));

            UpdateLobbyMemberList();
        }

        private void OnLobbyMembersChanged()
        {
            UpdateLobbyMemberList();
        }

        private void OnLobbyMemberConnectionChanged()
        {
            if (lobbyManager.CurrentLobby != null)
            {
                connectedClientsListTitle.text = $"Members of \"{lobbyManager.CurrentLobby.Name}\"";
                UpdateLobbyMemberList();
            }
            else
            {
                connectedClientsListTitle.text = "Not connected";
                UpdateLobbyMemberList();
            }
        }

        private void UpdateLobbyMemberList()
        {
            connectedClientsListScrollView.Clear();
            entryControls.Clear();

            if (networkManager.IsServer)
            {
                FillConnectedClientList(lobbyMemberManager.GetLobbyMembers().ToList());
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
                        CurrentLobbyMembersResponseDto responseDto = JsonConverter.FromJson<CurrentLobbyMembersResponseDto>(response);
                        FillConnectedClientList(responseDto.LobbyMembers);
                    });
            }
        }

        private void FillConnectedClientList(List<LobbyMember> lobbyMembers)
        {
            foreach (LobbyMember lobbyMember in lobbyMembers)
            {
                CreateConnectedClientEntryControl(lobbyMember);
            }

            ThemeManager.ApplyThemeSpecificStylesToVisualElements(connectedClientsListScrollView);
        }

        private void CreateConnectedClientEntryControl(LobbyMember lobbyMember)
        {
            VisualElement visualElement = connectedClientEntryUi.CloneTreeAndGetFirstChild();
            connectedClientsListScrollView.Add(visualElement);

            LobbyMemberUiControl entryControl = injector
                .WithBindingForInstance(lobbyMember)
                .WithBinding(new Binding(typeof(ILobbyManager), new ExistingInstanceProvider<ILobbyManager>(lobbyManager)))
                .WithBinding(new Binding(typeof(ILobbyMemberManager), new ExistingInstanceProvider<ILobbyMemberManager>(lobbyMemberManager)))
                .WithRootVisualElement(visualElement)
                .CreateAndInject<LobbyMemberUiControl>();

            entryControls.Add(entryControl);
        }

        public void Dispose()
        {
            disposables.ForEach(it => it.Dispose());
        }

        public VisualElement CreateVisualElement()
        {
            return Resources.Load<VisualTreeAsset>("CurrentSteamLobbyUi").CloneTreeAndGetFirstChild();
        }
    }
}
