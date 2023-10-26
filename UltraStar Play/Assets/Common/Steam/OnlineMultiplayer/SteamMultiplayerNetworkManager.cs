using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Netcode;


namespace SteamOnlineMultiplayer
{
    public class SteamMultiplayerNetworkManager : MonoBehaviour
    {
        public static SteamMultiplayerNetworkManager Instance { get; private set; } = null;

        /// NETWORK CALLBACKS ///
        public static event UnityAction OnClientReadied;

        public static event UnityAction OnClientDisconnectRequested;
        public static event UnityAction<ulong, int> OnClientSceneChanged;
        public static event UnityAction<SteamMultiplayerNetworkExtensions.ConnectStatus> OnConnectionCompleted;
        public static event UnityAction<SteamMultiplayerNetworkExtensions.ConnectStatus> OnDisconnectReceived;

        /// STEAM CALLBACKS ///
        public static event UnityAction OnLobbyCreatedEvent;

        public static event UnityAction OnLobbyDataChangedEvent;
        public static event UnityAction<string> OnLobbyChatMessageDeliveredEvent;
        public static event UnityAction<Friend, string> OnLobbyChatMessageReceivedEvent;

        public static event UnityAction<Friend> OnMemberDataChangedEvent;
        public static event UnityAction<Friend> OnMemberJoinedEvent;
        public static event UnityAction<Friend> OnMemberLeftEvent;
        public static event UnityAction<Friend, Lobby> OnMemberInviteReceivedEvent;
        public static event UnityAction<Friend, Friend> OnMemberKickedEvent;
        public static event UnityAction<Friend, Friend> OnMemberBannedEvent;

        public Lobby? CurrentLobby { get; private set; } = null;

        public void SetLobby(Lobby lobby) => CurrentLobby = lobby;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            //if (Instance != null && Instance != this)
            //{
            //    Destroy(gameObject);
            //    return;
            //}

            //Instance = this;
            //DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyMemberKicked += OnLobbyMemberKicked;
            SteamMatchmaking.OnLobbyMemberBanned += OnLobbyMemberBanned;
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamMatchmaking.OnChatMessage += OnChatMessage;
            SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
            SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;

            SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;

            NetworkManager.Singleton.OnServerStarted += OnConnectedCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }

        private void OnDestroy()
        {
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyMemberKicked -= OnLobbyMemberKicked;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamMatchmaking.OnChatMessage -= OnChatMessage;
            SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;

            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnConnectedCallback;
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;

                if (NetworkManager.Singleton.SceneManager != null)
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;

                if (NetworkManager.Singleton.CustomMessagingManager == null)
                    return;

                UnregisterClientMessageHandlers();
            }
        }

        private void OnApplicationQuit() => CurrentLobby?.Leave();

        public async Task<bool> StartSteamHost(SteamOnlineMultiplayerExtensions.LobbyConfig config)
        {
            if (NetworkManager.Singleton == null
                || NetworkManager.Singleton.IsServer)
            {
                return await Task.FromResult(false);
            }

            if (!SteamManager.Instance.IsConnectedToSteam)
                return await Task.FromResult(false);

            if (!StartNetworkHost())
            {
                Debug.LogError("Failed to start host server", this);
                return await Task.FromResult(false);
            }

            await CreateLobbyAsync(config);

            return await Task.FromResult(true);
        }

        public async Task<bool> StartSteamServer(SteamOnlineMultiplayerExtensions.LobbyConfig config)
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
                return await Task.FromResult(false);

            if (!StartNetworkServer())
            {
                Debug.LogError("Failed to start decided server", this);
                return await Task.FromResult(false);
            }

            await CreateLobbyAsync(config);

            return await Task.FromResult(true);
        }

        private async Task CreateLobbyAsync(SteamOnlineMultiplayerExtensions.LobbyConfig config)
        {
            CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(config.maxMembers);

            CurrentLobby?.SetVisibility(config.visibility);
            CurrentLobby?.SetJoinable(config.joinable);
            CurrentLobby?.SetData("name", config.name);
            CurrentLobby?.SetData("game", SteamManager.MelodyManiaSteamAppId.ToString());
            CurrentLobby?.SetData("isRunning", $"{false}");
        }

        public bool StartNetworkHost()
        {
            if (NetworkManager.Singleton.StartHost())
            {
                RegisterClientMessageHandlers();
                return true;
            }

            return false;
        }

        public bool StartNetworkServer()
        {
            if (NetworkManager.Singleton.StartServer())
            {
                RegisterClientMessageHandlers();
                return true;
            }

            return false;
        }

        public void RequestDisconnect()
        {
            if (NetworkManager.Singleton.IsServer)
                KickAllClients();

            CurrentLobby?.Leave();
            OnClientDisconnectRequested?.Invoke();

            void KickAllClients()
            {
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    var client = NetworkManager.Singleton.ConnectedClientsList[i];

                    if (client.ClientId.Equals(NetworkManager.Singleton.LocalClient.ClientId))
                        continue;

                    ServerToClientSetDisconnectReason(client.ClientId,
                        SteamMultiplayerNetworkExtensions.ConnectStatus.KickDisconnect);
                    //NW_ServerManager.Instance.KickClient(client.ClientId);
                }
            }
        }

        #region Network Callbacks

        private void HandleClientConnected(ulong clientId)
        {
            print("client connected!");
            if (clientId != NetworkManager.Singleton.LocalClientId)
                return;

            print($"client ID: {clientId}");
            OnConnectedCallback();
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }

        private void HandleSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete)
                return;

            OnClientSceneChanged?.Invoke(sceneEvent.ClientId,
                SceneManager.GetSceneByName(sceneEvent.SceneName).buildIndex);
        }

        private void OnConnectedCallback()
        {
            if (NetworkManager.Singleton.IsHost)
                OnConnectionCompleted?.Invoke(SteamMultiplayerNetworkExtensions.ConnectStatus.Success);

            OnClientReadied?.Invoke();
            print("Client Ready...");
        }

        #endregion

        #region Steam Callbacks

        private void OnGameRichPresenceJoinRequested(Friend friend, string key) =>
            Debug.Log($"{friend.Name} joined with key={key}");

        private void OnChatMessage(Lobby lobby, Friend friend, string msg) =>
            OnLobbyChatMessageReceivedEvent?.Invoke(friend, msg);

        private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend) => OnMemberDataChangedEvent?.Invoke(friend);

        private void OnLobbyDataChanged(Lobby lobby) => OnLobbyDataChangedEvent?.Invoke();

        private void OnLobbyMemberJoined(Lobby lobby, Friend friend) => OnMemberJoinedEvent?.Invoke(friend);

        private void OnLobbyMemberLeave(Lobby lobby, Friend friend) => OnMemberLeftEvent?.Invoke(friend);

        private void OnLobbyMemberKicked(Lobby lobby, Friend friend, Friend user) =>
            OnMemberKickedEvent?.Invoke(friend, user);

        private void OnLobbyMemberBanned(Lobby lobby, Friend friend, Friend user) =>
            OnMemberBannedEvent?.Invoke(friend, user);

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend) => OnMemberLeftEvent?.Invoke(friend);

        private void OnLobbyInvite(Friend friend, Lobby lobby) => OnMemberInviteReceivedEvent?.Invoke(friend, lobby);

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                Debug.LogError($"Lobby couldn't be created, {result}", this);
                return;
            }

            OnLobbyCreatedEvent?.Invoke();
            OnMemberJoinedEvent?.Invoke(lobby.Owner);
        }

        #endregion

        #region Message Handlers

        private void RegisterClientMessageHandlers()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(ServerToClientConnectResult), (senderClientId, messagePayload) =>
                {
                    messagePayload.ReadValueSafe(out SteamMultiplayerNetworkExtensions.ConnectStatus status);
                    Debug.Log($"{senderClientId} -> {status}", this);
                    OnConnectionCompleted?.Invoke(status);
                });

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                nameof(ServerToClientSetDisconnectReason), (senderClientId, messagePayload) =>
                {
                    messagePayload.ReadValueSafe(out SteamMultiplayerNetworkExtensions.ConnectStatus status);
                    Debug.Log($"{senderClientId} -> {status}", this);
                    OnDisconnectReceived?.Invoke(status);
                });
        }

        private void UnregisterClientMessageHandlers()
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
                nameof(ServerToClientConnectResult));

            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
                nameof(ServerToClientSetDisconnectReason));
        }

        #endregion

        #region Message Senders

        public void ServerToClientConnectResult(ulong netId, SteamMultiplayerNetworkExtensions.ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(SteamMultiplayerNetworkExtensions.ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);

            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ServerToClientConnectResult), netId,
                writer);
        }

        public void ServerToClientSetDisconnectReason(ulong netId,
            SteamMultiplayerNetworkExtensions.ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(SteamMultiplayerNetworkExtensions.ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);

            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ServerToClientSetDisconnectReason),
                netId, writer);
        }

        #endregion
    }
}
