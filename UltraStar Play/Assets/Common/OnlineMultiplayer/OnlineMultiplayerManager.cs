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

        private readonly Subject<LobbyMemberConnectionChangedEvent> lobbyMemberConnectionChangedEventSteam = new();
        public IObservable<LobbyMemberConnectionChangedEvent> LobbyMemberConnectionChangedEventSteam => lobbyMemberConnectionChangedEventSteam
            .ObserveOnMainThread();

        private ILobbyManager LobbyManager => BackendManager.CurrentBackend.LobbyManager;
        private ILobbyMemberManager LobbyMemberManager => BackendManager.CurrentBackend.LobbyMemberManager;

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
    }
}
