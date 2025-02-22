using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class OnlineMultiplayerManager : AbstractSingletonBehaviour, INeedInjection, IInjectionFinishedListener
    {
        public static OnlineMultiplayerManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<OnlineMultiplayerManager>();

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private Settings settings;

        [Inject]
        private NonPersistentSettings nonPersistentSettings;

        [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
        public OnlineMultiplayerBackendManager BackendManager { get; private set; }

        private readonly Subject<LobbyMemberConnectionChangedEvent> lobbyMemberConnectionChangedEventSteam = new();
        public IObservable<LobbyMemberConnectionChangedEvent> LobbyMemberConnectionChangedEventSteam => lobbyMemberConnectionChangedEventSteam
            .ObserveOnMainThread();

        private readonly Subject<OwnNetcodeClientStartedEvent> ownNetcodeClientStartedEventStream = new();
        public IObservable<OwnNetcodeClientStartedEvent> OwnNetcodeClientStartedEventStream => ownNetcodeClientStartedEventStream
            .ObserveOnMainThread();

        private readonly Subject<OwnNetcodeClientStoppedEvent> ownNetcodeClientStoppedEventStream = new();
        public IObservable<OwnNetcodeClientStoppedEvent> OwnNetcodeClientStoppedEventStream => ownNetcodeClientStoppedEventStream
            .ObserveOnMainThread();

        public ILobbyManager LobbyManager => BackendManager.CurrentBackend.LobbyManager;
        public ILobbyMemberManager LobbyMemberManager => BackendManager.CurrentBackend.LobbyMemberManager;

        /**
         * Each connected Netcode peer has a client, but only one of them is the host and server.
         */
        public bool IsOnlineGame => networkManager != null && networkManager.IsClient;

        /**
         * The host is the Netcode server that adds a client for itself automatically.
         */
        public bool IsHost => networkManager != null && networkManager.IsHost;

        public LobbyMember OwnLobbyMember => networkManager != null
            ? LobbyMemberManager.GetLobbyMember(networkManager.LocalClientId)
            : null;
        public PlayerProfile OwnLobbyMemberPlayerProfile
        {
            get
            {
                LobbyMember ownLobbyMember = OwnLobbyMember;
                if (ownLobbyMember == null)
                {
                    return null;
                }
                return nonPersistentSettings.LobbyMemberPlayerProfiles
                    .FirstOrDefault(it => it.UnityNetcodeClientId == ownLobbyMember.UnityNetcodeClientId);
            }
        }
        public NetworkObject OwnLobbyMemberNetworkObject => networkManager != null
            ? networkManager.SpawnManager.GetLocalPlayerObject()
            : null;
        public UnityNetcodeClientId OwnLobbyMemberUnityNetcodeClientId => networkManager != null
            ? networkManager.LocalClientId
            : 0;

        public IReadOnlyList<ulong> OtherLobbyMembersUnityNetcodeClientIds
        {
            get
            {
                return AllLobbyMembersUnityNetcodeClientIds
                    .Except(new List<ulong>() { OwnLobbyMemberUnityNetcodeClientId })
                    .ToList();
            }
        }

        public IReadOnlyList<ulong> AllLobbyMembersUnityNetcodeClientIds
        {
            get
            {
                return nonPersistentSettings.LobbyMemberPlayerProfiles
                    .Select(it => it.UnityNetcodeClientId.Value)
                    .ToList();
            }
        }

        private DelayedMessagingControl messagingControl;
        public IMessagingControl MessagingControl => messagingControl;
        public ObservableMessagingControl ObservableMessagingControl { get; private set; }

        private bool isInitialized;

        protected override object GetInstance()
        {
            return Instance;
        }

        public void OnInjectionFinished()
        {
            // Injection is done per-scene, but initialization should be done only once and before Start is called in other scripts.
            Init();
        }

        protected override void StartSingleton()
        {
            networkManager.OnClientConnectedCallback += OnNetcodeClientConnectedOnServerOrLocal;
            networkManager.OnClientDisconnectCallback += OnNetcodeClientDisconnectedOnServerOrLocal;
            networkManager.ConnectionApprovalCallback += OnNetcodeClientConnectionApproval;

            networkManager.OnClientStarted += OnNetcodeLocalClientStarted;
            networkManager.OnClientStopped += OnNetcodeLocalClientStopped;

            networkManager.OnServerStarted += OnNetcodeLocalServerStarted;
            networkManager.OnServerStopped += OnNetcodeLocalServerStopped;

            LobbyMemberConnectionChangedEventSteam
                .Subscribe(evt => UpdateLobbyMemberPlayerProfiles());

            settings.ObserveEveryValueChanged(it => it.OnlineMultiplayerSimulatedJitterInMillis)
                .Subscribe(newValue => messagingControl.DelayInMillis = newValue);
        }

        protected override void OnDestroySingleton()
        {
            networkManager.OnClientConnectedCallback -= OnNetcodeClientConnectedOnServerOrLocal;
            networkManager.OnClientDisconnectCallback -= OnNetcodeClientDisconnectedOnServerOrLocal;
            networkManager.ConnectionApprovalCallback -= OnNetcodeClientConnectionApproval;

            networkManager.OnClientStarted -= OnNetcodeLocalClientStarted;
            networkManager.OnClientStopped -= OnNetcodeLocalClientStopped;

            networkManager.OnServerStarted -= OnNetcodeLocalServerStarted;
            networkManager.OnServerStopped -= OnNetcodeLocalServerStopped;
        }

        private void Update()
        {
            ObservableMessagingControl.UpdateMessageTimeout();
        }

        private void OnNetcodeLocalServerStarted()
        {
            Debug.Log($"OnNetcodeLocalServerStarted");
            MessagingControl.RegisterNamedMessageHandlersToForwardMessages();
        }

        private void OnNetcodeLocalClientStarted()
        {
            Debug.Log($"OnNetcodeLocalClientStarted");
            ownNetcodeClientStartedEventStream.OnNext(new OwnNetcodeClientStartedEvent());
        }

        private void OnNetcodeLocalClientStopped(bool wasHostMode)
        {
            Debug.Log($"OnNetcodeLocalClientStopped(wasHostMode: {wasHostMode})");
            MessagingControl.ClearNamedMessageHandlers();
            NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_error_disconnected));

            if (LobbyManager.CurrentLobby != null)
            {
                LobbyManager.LeaveCurrentLobby();
            }

            if (wasHostMode)
            {
                LobbyMemberManager.ClearLobbyMemberRegistry();
            }

            ownNetcodeClientStoppedEventStream.OnNext(new OwnNetcodeClientStoppedEvent());
        }

        private void OnNetcodeLocalServerStopped(bool wasHostMode)
        {
            Debug.Log($"OnNetcodeLocalServerStopped(wasHostMode: {wasHostMode})");

            if (LobbyManager.CurrentLobby != null)
            {
                LobbyManager.LeaveCurrentLobby();
            }

            if (wasHostMode)
            {
                LobbyMemberManager.ClearLobbyMemberRegistry();
            }
        }

        private void OnNetcodeClientConnectedOnServerOrLocal(ulong netcodeClientId)
        {
            if (!networkManager.IsServer)
            {
                return;
            }

            Debug.Log($"OnNetcodeClientConnectedOnServerOrLocal(UnityNetcodeClientId: {netcodeClientId})");
        }

        private void OnNetcodeClientDisconnectedOnServerOrLocal(ulong netcodeClientId)
        {
            if (!networkManager.IsServer)
            {
                return;
            }

            Debug.Log($"OnNetcodeClientDisconnectedOnServerOrLocal(UnityNetcodeClientId: {netcodeClientId})");
            LobbyMemberManager.RemoveLobbyMemberFromRegistry(netcodeClientId);
        }

        public void OnLobbyMemberNetworkObjectSpawned(ulong netcodeClientId)
        {
            LobbyMemberManager.UpdateLobbyMemberRegistry();
            lobbyMemberConnectionChangedEventSteam.OnNext(new LobbyMemberConnectedEvent(netcodeClientId));
        }

        public void OnLobbyMemberNetworkObjectDestroyed(ulong netcodeClientId)
        {
            // The object has not been destroyed yet. Wait one frame to finish destruction.
            AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () =>
            {
                LobbyMemberManager.UpdateLobbyMemberRegistry();
                lobbyMemberConnectionChangedEventSteam.OnNext(new LobbyMemberDisconnectedEvent(netcodeClientId));
            });
        }

        private void OnNetcodeClientConnectionApproval(
            NetworkManager.ConnectionApprovalRequest connectionApprovalRequest,
            NetworkManager.ConnectionApprovalResponse response)
        {
            LobbyMemberManager.OnNetcodeClientConnectionApproval(connectionApprovalRequest, response);
        }

        private void Init()
        {
            if (isInitialized)
            {
                return;
            }
            isInitialized = true;

            MessagingControl regularMessagingControl = new MessagingControl(networkManager);
            messagingControl = new DelayedMessagingControl(regularMessagingControl);
            ObservableMessagingControl = new ObservableMessagingControl(messagingControl);
        }

        private void UpdateLobbyMemberPlayerProfiles()
        {
            IReadOnlyList<LobbyMember> lobbyMembers = LobbyMemberManager.GetLobbyMembers();
            nonPersistentSettings.LobbyMemberPlayerProfiles = lobbyMembers
                .Select(it => new LobbyMemberPlayerProfile(it.DisplayName, it.UnityNetcodeClientId))
                .ToList();
        }
    }
}
