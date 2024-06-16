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

        [Inject(UxmlName = R.UxmlNames.lobbyInfoContainer)]
        private VisualElement lobbyInfoContainer;

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
            lobbyInfoContainer.HideByDisplay();

            connectedClientsListTitle.SetTranslatedText(Translation.Get(R.Messages.onlineGame_lobby_title,
                "lobbyName", lobbyManager.CurrentLobby?.Name ?? ""));
            connectedClientsListScrollView.Clear();

            disconnectOnlineGameButton.RegisterCallbackButtonTriggered(_ => lobbyManager.LeaveCurrentLobby());

            disposables.Add(onlineMultiplayerManager
                .LobbyMemberConnectionChangedEventSteam
                .Subscribe(evt =>
                {
                    OnLobbyMemberConnectionChanged();
                }));

            UpdateLobbyMemberList();
        }

        private void OnLobbyMemberConnectionChanged()
        {
            if (lobbyManager.CurrentLobby != null)
            {
                connectedClientsListTitle.SetTranslatedText(Translation.Get(R.Messages.onlineGame_lobby_title,
                    "lobbyName", lobbyManager.CurrentLobby.Name));
                UpdateLobbyMemberList();
            }
            else
            {
                connectedClientsListTitle.SetTranslatedText(Translation.Get(R.Messages.onlineGame_lobby_list_title));
                UpdateLobbyMemberList();
            }
        }

        private void UpdateLobbyMemberList()
        {
            FillConnectedClientList(lobbyMemberManager.GetLobbyMembers().ToList());
        }

        private void FillConnectedClientList(List<LobbyMember> lobbyMembers)
        {
            connectedClientsListScrollView.Clear();
            entryControls.Clear();

            foreach (LobbyMember lobbyMember in lobbyMembers)
            {
                CreateConnectedClientEntryControl(lobbyMember);
            }
        }

        private void CreateConnectedClientEntryControl(LobbyMember lobbyMember)
        {
            VisualElement visualElement = connectedClientEntryUi.CloneTreeAndGetFirstChild();
            connectedClientsListScrollView.Add(visualElement);

            LobbyMemberUiControl entryControl = injector
                .WithBindingForInstance(lobbyMember)
                .WithBinding(new UniInjectBinding(typeof(ILobbyManager), new ExistingInstanceProvider<ILobbyManager>(lobbyManager)))
                .WithBinding(new UniInjectBinding(typeof(ILobbyMemberManager), new ExistingInstanceProvider<ILobbyMemberManager>(lobbyMemberManager)))
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
