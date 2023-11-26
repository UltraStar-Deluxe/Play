using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class OnlineMultiplayerManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static OnlineMultiplayerManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<OnlineMultiplayerManager>();

        [Inject]
        private NetworkManager networkManager;

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
        public bool IsOnlineGame => networkManager.IsClient;
        public bool IsLocalGame => !IsOnlineGame;

        /**
         * The host is the Netcode server that adds a client for itself automatically.
         */
        public bool IsServer => networkManager.IsServer;

        public LobbyMember OwnLobbyMember => LobbyMemberManager.GetLobbyMember(networkManager.LocalClientId);
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
        public NetworkObject OwnLobbyMemberNetworkObject => networkManager.SpawnManager.GetLocalPlayerObject();
        public UnityNetcodeClientId OwnLobbyMemberUnityNetcodeClientId => OwnLobbyMember.UnityNetcodeClientId;

        public IReadOnlyList<ulong> OtherLobbyMemberUnityNetcodeClientIds
        {
            get
            {
                return AllLobbyMemberUnityNetcodeClientIds
                    .Except(new List<ulong>() { OwnLobbyMemberUnityNetcodeClientId })
                    .ToList();
            }
        }

        public IReadOnlyList<ulong> AllLobbyMemberUnityNetcodeClientIds
        {
            get
            {
                return nonPersistentSettings.LobbyMemberPlayerProfiles
                    .Select(it => it.UnityNetcodeClientId.Value)
                    .ToList();
            }
        }

        public MessagingControl MessagingControl { get; private set; }
        public ObservableMessagingControl ObservableMessagingControl { get; private set; }

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void AwakeSingleton()
        {
            MessagingControl = new MessagingControl(
                () => OwnLobbyMemberUnityNetcodeClientId,
                () => OtherLobbyMemberUnityNetcodeClientIds);

            ObservableMessagingControl = new ObservableMessagingControl(
                MessagingControl,
                () => OwnLobbyMemberUnityNetcodeClientId,
                () => OtherLobbyMemberUnityNetcodeClientIds);
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
            MessagingControl.RegisterNamedMessageHandlersToForwardMessagesIfNeeded();
        }

        private void UpdateLobbyMemberPlayerProfiles()
        {
            IReadOnlyList<LobbyMember> lobbyMembers = LobbyMemberManager.GetLobbyMembers();
            nonPersistentSettings.LobbyMemberPlayerProfiles = lobbyMembers
                .Select(it => new LobbyMemberPlayerProfile(it.DisplayName, it.UnityNetcodeClientId))
                .ToList();
        }

        private void OnNetcodeLocalClientStarted()
        {
            Debug.Log($"OnNetcodeLocalClientStarted");
            ownNetcodeClientStartedEventStream.OnNext(new OwnNetcodeClientStartedEvent());
        }

        private void OnNetcodeLocalClientStopped(bool wasHostMode)
        {
            Debug.Log($"OnNetcodeLocalClientStopped(wasHostMode: {wasHostMode})");
            UiManager.CreateNotification("Disconnected from online game");

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
            lobbyMemberConnectionChangedEventSteam.OnNext(new LobbyMemberConnectedEvent(netcodeClientId));
        }

        public void OnLobbyMemberNetworkObjectDestroyed(ulong netcodeClientId)
        {
            lobbyMemberConnectionChangedEventSteam.OnNext(new LobbyMemberDisconnectedEvent(netcodeClientId));
        }

        private void OnNetcodeClientConnectionApproval(
            NetworkManager.ConnectionApprovalRequest connectionApprovalRequest,
            NetworkManager.ConnectionApprovalResponse response)
        {
            LobbyMemberManager.OnNetcodeClientConnectionApproval(connectionApprovalRequest, response);
        }

        // public T GetNetworkBehaviour<T>(UnityNetcodeClientId netcodeClientId)
        //     where T : NetworkBehaviour
        // {
        //     GameObject lobbyMemberGameObject = networkManager.SpawnManager.GetPlayerNetworkObject(netcodeClientId).gameObject;
        //     return lobbyMemberGameObject.GetComponentInChildren<T>();
        // }
        //
        // public T GetNetworkBehaviourOfOwnLobbyMember<T>()
        //     where T : NetworkBehaviour
        // {
        //     return GetNetworkBehaviour<T>(OwnLobbyMemberNetworkObject.OwnerClientId);
        // }
        //
        // public T AddNetworkBehaviourIfMissing<T>(UnityNetcodeClientId netcodeClientId)
        //     where T : NetworkBehaviour
        // {
        //     NetworkObject networkObject = networkManager.SpawnManager.GetPlayerNetworkObject(netcodeClientId);
        //     if (networkObject == null)
        //     {
        //         throw new OnlineMultiplayerException($"Cannot add component of type {typeof(T)} because no lobby member found with Netcode id {netcodeClientId}");
        //     }
        //
        //     T existingComponent = networkObject.GetComponentInChildren<T>();
        //     if (existingComponent)
        //     {
        //         return existingComponent;
        //     }
        //
        //     GameObject newGameObject = new();
        //     newGameObject.name = typeof(T).Name;
        //     newGameObject.transform.parent = networkObject.transform;
        //     return newGameObject.AddComponent<T>();
        // }
    }
}
