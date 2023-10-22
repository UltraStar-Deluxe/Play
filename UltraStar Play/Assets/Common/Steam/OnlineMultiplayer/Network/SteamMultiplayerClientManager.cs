using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Netcode.Transports.Facepunch;

using static Network.Framework.SteamMultiplayerNetworkExtensions;
using System.Threading.Tasks;

namespace Network.Framework
{
    [AddComponentMenu("Network/Framework/Client Manager"), DisallowMultipleComponent]
    [RequireComponent(typeof(SteamMultiplayerNetworkManager))]
    public class SteamMultiplayerClientManager : MonoBehaviour
    {
        public static SteamMultiplayerClientManager Instance { get; private set; } = null;

        public DisconnectReason DisconnectReason { get; private set; } = new DisconnectReason();

        public static event UnityAction<ConnectStatus> OnConnectionFinished;
        public static event UnityAction OnNetworkTimedOut;

        private SteamMultiplayerNetworkManager portal = null;
        private FacepunchTransport transport = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            portal = GetComponent<SteamMultiplayerNetworkManager>();
            transport = GetComponent<FacepunchTransport>();

            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

            DisconnectReason.OnReasonChanged += OnReasonChanged;
            SteamMultiplayerNetworkManager.OnClientReadied += OnNetworkReadied;
            SteamMultiplayerNetworkManager.OnConnectionCompleted += OnClientConnectionFinished;
            SteamMultiplayerNetworkManager.OnDisconnectReceived += OnDisconnectReasonReceived;

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        private void OnDestroy()
        {
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

            DisconnectReason.OnReasonChanged -= OnReasonChanged;
            SteamMultiplayerNetworkManager.OnClientReadied -= OnNetworkReadied;
            SteamMultiplayerNetworkManager.OnConnectionCompleted -= OnClientConnectionFinished;
            SteamMultiplayerNetworkManager.OnDisconnectReceived -= OnDisconnectReasonReceived;

            if (NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        /// <summary>
        /// Start client [With Steam Transport]
        /// </summary>
        /// <param name="lobby"></param>
        /// <returns></returns>
        public bool StartSteamClient(SteamId targetId)
        {
            transport.targetSteamId = targetId;
            return StartNetworkClient();
        }

        /// <summary>
        /// Start client
        /// </summary>
        /// <returns></returns>
        public bool StartNetworkClient()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                clientGUID = System.Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().buildIndex,
                displayName = PlayerPrefs.GetString("PlayerName", "Missing Name")
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
            return NetworkManager.Singleton.StartClient();
        }

        #region Steam Callbacks

        private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
        {
            portal.SetLobby(lobby);
            var result = await lobby.Join();

            if (result != RoomEnter.Success)
            {
                Debug.LogError($"Couldn't enter the lobby, {result}", this);
                return;
            }

            StartSteamClient(friendId);
        }

        #endregion

        #region Network Callbacks

        private void OnNetworkReadied()
        {
            if (!NetworkManager.Singleton.IsClient)
                return;

            if (!NetworkManager.Singleton.IsHost)
                SteamMultiplayerNetworkManager.OnClientDisconnectRequested += OnUserDisconnectRequested;
        }

        private void OnUserDisconnectRequested()
        {
            Debug.Log($"You have disconnected from the server", this);

            DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);
            NetworkManager.Singleton.Shutdown();

            OnClientDisconnect(NetworkManager.Singleton.LocalClientId);

            SceneNavigator.Instance.LoadScene(EScene.MainScene);
        }

        private void OnClientConnectionFinished(ConnectStatus status)
        {
            if (status != ConnectStatus.Success)
                DisconnectReason.SetDisconnectReason(status);

            OnConnectionFinished?.Invoke(status);
        }

        private void OnDisconnectReasonReceived(ConnectStatus status)
        {
            Debug.Log($"You have been disconnected by {status}", this);
            DisconnectReason.SetDisconnectReason(status);
        }

        private void OnReasonChanged(ConnectStatus status)
        {
            Debug.Log($"{nameof(OnReasonChanged)} -> {status}", this);

            switch (status)
            {
                case ConnectStatus.KickDisconnect: OnClientKicked(); break;
            }

            void OnClientKicked()
            {
                Debug.Log("You have been kicked", this);
                portal.CurrentLobby?.Leave();
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            portal.CurrentLobby?.Leave();

            if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
            {
                SteamMultiplayerNetworkManager.OnClientDisconnectRequested -= OnUserDisconnectRequested;

                if (SceneNavigator.Instance.CurrentScene != EScene.MainScene)
                {
                    if (!DisconnectReason.HasTransitionReason)
                        DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);

                    // Debug.Log("ClientDisconnect, opening main scene.");
                    // SceneNavigator.Instance.LoadScene(EScene.MainScene);
                }
                else
                {
                    OnNetworkTimedOut?.Invoke();
                }
            }
        }

        #endregion
    }
}
