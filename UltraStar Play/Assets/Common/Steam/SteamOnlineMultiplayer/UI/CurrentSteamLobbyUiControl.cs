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
    public class CurrentSteamLobbyUiControl : INeedInjection, IInjectionFinishedListener, ICurrentLobbyUiControl
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
        private SteamLobbyMemberManager steamLobbyMemberManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        [Inject]
        private Injector injector;

        private readonly List<IDisposable> disposables = new();

        private readonly List<SteamLobbyMemberUiControl> entryControls = new();

        public void OnInjectionFinished()
        {
            connectedClientsListTitle.text = "Connected Players";
            connectedClientsListScrollView.Clear();

            disconnectOnlineGameButton.RegisterCallbackButtonTriggered(_ => steamLobbyManager.LeaveCurrentLobby());

            disposables.Add(onlineMultiplayerManager
                .LobbyMemberConnectionChangedEventSteam
                .Subscribe(_ => OnLobbyMembersChanged()));

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

            UpdateLobbyMemberList();
        }

        private void OnLobbyMembersChanged()
        {
            UpdateLobbyMemberList();
        }

        private void OnSteamLobbyChanged()
        {
            if (steamLobbyManager.CurrentSteamLobby != null)
            {
                connectedClientsListTitle.text = $"Members of \"{steamLobbyManager.CurrentSteamLobby.Name}\"";
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
            FillConnectedClientList(steamLobbyMemberManager.GetSteamLobbyMembers().ToList());
        }

        private void FillConnectedClientList(List<SteamLobbyMember> memberDatas)
        {
            connectedClientsListScrollView.Clear();
            entryControls.Clear();

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

            SteamLobbyMemberUiControl entryControl = injector
                .WithRootVisualElement(visualElement)
                .WithBinding(new UniInjectBinding(typeof(ILobbyManager), new ExistingInstanceProvider<ILobbyManager>(steamLobbyManager)))
                .WithBinding(new UniInjectBinding(typeof(ILobbyMemberManager), new ExistingInstanceProvider<ILobbyMemberManager>(steamLobbyMemberManager)))
                .WithBinding(new UniInjectBinding(typeof(LobbyMember), new ExistingInstanceProvider<LobbyMember>(steamLobbyMember)))
                .WithBindingForInstance(steamLobbyMember)
                .CreateAndInject<SteamLobbyMemberUiControl>();

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
