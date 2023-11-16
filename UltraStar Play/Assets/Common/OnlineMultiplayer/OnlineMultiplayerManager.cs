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

        [Inject]
        public NetcodeRequestHandlerRegistry NetcodeRequestHandlerRegistry { get; private set; }

        private readonly Subject<LobbyMemberConnectionChangedEvent> lobbyMemberConnectionChangedEventSteam = new();
        public IObservable<LobbyMemberConnectionChangedEvent> LobbyMemberConnectionChangedEventSteam => lobbyMemberConnectionChangedEventSteam
            .ObserveOnMainThread();

        public ILobbyManager LobbyManager => BackendManager.CurrentBackend.LobbyManager;
        public ILobbyMemberManager LobbyMemberManager => BackendManager.CurrentBackend.LobbyMemberManager;

        /**
         * Each connected Netcode peer has a client, but only one of them is the host and server.
         */
        public bool IsConnectedToOnlineGame => networkManager.IsClient;

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
                return  nonPersistentSettings.LobbyMemberPlayerProfiles
                    .FirstOrDefault(it => it.UnityNetcodeClientId == ownLobbyMember.UnityNetcodeClientId);
            }
        }

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            networkManager.OnClientConnectedCallback += OnNetcodeClientConnectedOnServerOrLocal;
            networkManager.OnClientDisconnectCallback += OnNetcodeClientDisconnectedOnServerOrLocal;
            networkManager.ConnectionApprovalCallback += OnNetcodeClientConnectionApproval;
            networkManager.OnClientStopped += OnNetcodeLocalClientStopped;
            networkManager.OnServerStopped += OnNetcodeLocalServerStopped;

            LobbyMemberConnectionChangedEventSteam
                .Subscribe(evt => UpdateLobbyMemberPlayerProfiles());
        }

        private void UpdateLobbyMemberPlayerProfiles()
        {
            IReadOnlyList<LobbyMember> lobbyMembers = LobbyMemberManager.GetLobbyMembers();
            nonPersistentSettings.LobbyMemberPlayerProfiles = lobbyMembers
                .Select(it => new LobbyMemberPlayerProfile(it.DisplayName, it.UnityNetcodeClientId))
                .ToList();
        }

        protected override void OnDestroySingleton()
        {
            networkManager.OnClientConnectedCallback -= OnNetcodeClientConnectedOnServerOrLocal;
            networkManager.OnClientDisconnectCallback -= OnNetcodeClientDisconnectedOnServerOrLocal;
            networkManager.ConnectionApprovalCallback -= OnNetcodeClientConnectionApproval;
            networkManager.OnClientStopped -= OnNetcodeLocalClientStopped;
            networkManager.OnServerStopped -= OnNetcodeLocalServerStopped;
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

        public void SendMessageToServer(JsonSerializable jsonSerializable)
        {
            SendRequestToServerAsObservable<object>(jsonSerializable)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Exception when sending message to server: {ex.Message}. Sent message: {jsonSerializable.ToJson()}");
                })
                // Subscribe to trigger observable
                .Subscribe(responseDto =>
                {
                    // Do nothing.
                });
        }

        public IObservable<RESPONSEDTO> SendRequestToServerAsObservable<RESPONSEDTO>(JsonSerializable jsonSerializable)
            where RESPONSEDTO : new()
        {
            if (jsonSerializable == null)
            {
                return Observable.Empty<RESPONSEDTO>();
            }

            NetworkObject localPlayerObject = networkManager.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObject == null)
            {
                throw new OnlineMultiplayerException("Missing LocalPlayerObject");
            }

            LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = localPlayerObject.GetComponent<LobbyMemberNetworkBehaviour>();
            if (lobbyMemberNetworkBehaviour == null)
            {
                throw new OnlineMultiplayerException($"Missing {nameof(LobbyMemberMessagingNetworkBehaviour)}");
            }

            return lobbyMemberNetworkBehaviour.MessagingNetworkBehaviour
                .SendRequestToServerAsObservable(jsonSerializable.ToJson())
                .Select(responseMessage => JsonConverter.FromJson<RESPONSEDTO>(responseMessage));
        }

        public IObservable<RESPONSEDTO> SendMessageToAllClientsAsObservable<RESPONSEDTO>(JsonSerializable jsonSerializable)
            where RESPONSEDTO : new()
        {
            if (jsonSerializable == null)
            {
                return Observable.Empty<RESPONSEDTO>();
            }

            NetworkObject localPlayerObject = networkManager.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObject == null)
            {
                throw new OnlineMultiplayerException("Missing LocalPlayerObject");
            }

            LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = localPlayerObject.GetComponent<LobbyMemberNetworkBehaviour>();
            if (lobbyMemberNetworkBehaviour == null)
            {
                throw new OnlineMultiplayerException($"Missing {nameof(LobbyMemberMessagingNetworkBehaviour)}");
            }

            return lobbyMemberNetworkBehaviour.MessagingNetworkBehaviour
                .SendRequestMessageToAllClientsAsObservable(jsonSerializable.ToJson())
                .Select(responseMessage => JsonConverter.FromJson<RESPONSEDTO>(responseMessage));
        }

        public void SendMessageToAllClients(JsonSerializable jsonSerializable)
        {
            SendMessageToAllClientsAsObservable<object>(jsonSerializable)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to send message to clients: {ex.Message}. Message to be sent: {jsonSerializable.ToJson()}");
                })
                // Subscribe to trigger observable
                .Subscribe(responseDto =>
                {
                    // Do nothing.
                });
        }
    }
}
