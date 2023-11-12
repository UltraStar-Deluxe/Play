using System;
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

        [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
        public OnlineMultiplayerBackendManager BackendManager { get; private set; }

        [Inject]
        public NetcodeRequestHandlerRegistry NetcodeRequestHandlerRegistry { get; private set; }

        private readonly Subject<LobbyMemberConnectionChangedEvent> lobbyMemberConnectionChangedEventSteam = new();
        public IObservable<LobbyMemberConnectionChangedEvent> LobbyMemberConnectionChangedEventSteam => lobbyMemberConnectionChangedEventSteam
            .ObserveOnMainThread();

        public ILobbyManager LobbyManager => BackendManager.CurrentBackend.LobbyManager;
        public ILobbyMemberManager LobbyMemberManager => BackendManager.CurrentBackend.LobbyMemberManager;
        public bool IsConnectedToOnlineGame => networkManager.IsClient;
        public bool IsServer => networkManager.IsServer;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            networkManager.OnClientConnectedCallback += OnNetcodeClientConnected;
            networkManager.OnClientDisconnectCallback += OnNetcodeClientDisconnected;
            networkManager.ConnectionApprovalCallback += OnNetcodeClientConnectionApproval;
            networkManager.OnClientStopped += OnNetcodeLocalClientStopped;
            networkManager.OnServerStopped += OnNetcodeLocalServerStopped;
        }

        protected override void OnDestroySingleton()
        {
            networkManager.OnClientConnectedCallback -= OnNetcodeClientConnected;
            networkManager.OnClientDisconnectCallback -= OnNetcodeClientDisconnected;
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

        private void OnNetcodeClientConnected(ulong netcodeClientId)
        {
            Debug.Log($"OnNetcodeClientConnected(UnityNetcodeClientId: {netcodeClientId})");
            lobbyMemberConnectionChangedEventSteam.OnNext(new LobbyMemberConnectedEvent(netcodeClientId));
        }

        private void OnNetcodeClientDisconnected(ulong netcodeClientId)
        {
            Debug.Log($"OnNetcodeClientDisconnected(UnityNetcodeClientId: {netcodeClientId})");
            LobbyMemberManager.RemoveLobbyMemberFromRegistry(netcodeClientId);
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
            SendMessageToServerAsObservable<object>(jsonSerializable)
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

        public IObservable<T> SendMessageToServerAsObservable<T>(JsonSerializable jsonSerializable) where T : new()
        {
            if (jsonSerializable == null)
            {
                return Observable.Empty<T>();
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
                .Select(responseMessage => JsonConverter.FromJson<T>(responseMessage));
        }
    }
}
