using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SteamOnlineMultiplayer
{
    public class SteamMultiplayerServerManager : MonoBehaviour
    {
        private const int MaxConnectionPayload = 1024;

        public static SteamMultiplayerServerManager Instance { get; private set; } = null;

        public static event UnityAction OnGameInProgressChanged;
        public static event UnityAction OnServerStarted;
        public static event UnityAction OnServerShutdown;

        [Header("Config")] [SerializeField] byte m_MaxPlayers = 4;

        public bool GameInProgress
        {
            get => gameInProgress;
            set
            {
                OnGameInProgressChanged?.Invoke();
                gameInProgress = value;
            }
        }

        public IReadOnlyDictionary<FixedString64Bytes, MemberData> MemberLookup => members;
        public IReadOnlyDictionary<ulong, FixedString64Bytes> ClientLookup => clientIdToGuid;
        public IReadOnlyDictionary<SteamId, FixedString64Bytes> SteamLookup => steamIdToGuid;

        private Dictionary<FixedString64Bytes, MemberData> members =
            new(capacity: MaxConnectionPayload);

        private Dictionary<ulong, FixedString64Bytes> clientIdToGuid = new(capacity: MaxConnectionPayload);
        private Dictionary<SteamId, FixedString64Bytes> steamIdToGuid = new(capacity: MaxConnectionPayload);
        private Dictionary<ulong, int> clientSceneMap = new(capacity: MaxConnectionPayload);
        private bool gameInProgress = false;

        private SteamMultiplayerNetworkManager portal = null;

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

            SteamMultiplayerNetworkManager.OnClientReadied += HandleNetworkReadied;

            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

            members = new Dictionary<FixedString64Bytes, MemberData>();
            clientIdToGuid = new Dictionary<ulong, FixedString64Bytes>();
            steamIdToGuid = new Dictionary<SteamId, FixedString64Bytes>();
            clientSceneMap = new Dictionary<ulong, int>();
        }

        private void OnDestroy()
        {
            SteamMultiplayerNetworkManager.OnClientReadied -= HandleNetworkReadied;

            if (NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        }

        public MemberData? GetMemberData(ulong clientId)
        {
            if (clientIdToGuid.TryGetValue(clientId, out FixedString64Bytes clientGuid))
            {
                if (members.TryGetValue(clientGuid, out MemberData playerData))
                    return playerData;
                else
                    Debug.LogWarning($"No member data found for client id: {clientId}");
            }
            else
                Debug.LogWarning($"No client guid found for client id: {clientId}");

            return null;
        }

        public MemberData? GetMemberData(SteamId steamId)
        {
            if (steamIdToGuid.TryGetValue(steamId, out FixedString64Bytes clientGuid))
            {
                if (members.TryGetValue(clientGuid, out MemberData playerData))
                    return playerData;
                else
                    Debug.LogWarning($"No member data found for client id: {steamId}");
            }
            else
                Debug.LogWarning($"No client guid found for client id: {steamId}");

            return null;
        }

        public void KickClient(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            if (NetworkManager.Singleton.SpawnManager != null)
            {
                NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

                if (networkObject != null)
                    networkObject.Despawn(true);
            }

            NetworkManager.Singleton.DisconnectClient(clientId);
        }

        public bool StartGame()
        {
            if (!NetworkManager.Singleton.IsServer)
                return false;

            gameInProgress = true;
            Debug.Log("Starting online multiplayer game");
            return true;
        }

        public bool EndGame()
        {
            if (!NetworkManager.Singleton.IsServer)
                return false;

            gameInProgress = false;
            Debug.Log("Ending online multiplayer game");
            return true;
        }

        private void ClearData()
        {
            members.Clear();
            clientIdToGuid.Clear();
            clientSceneMap.Clear();

            gameInProgress = false;
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest,
            NetworkManager.ConnectionApprovalResponse callback)
        {
            byte[] connectionData = connectionApprovalRequest.Payload;
            ulong clientId = connectionApprovalRequest.ClientNetworkId;
            if (connectionData.Length > MaxConnectionPayload)
            {
                callback.CreatePlayerObject = false;
                callback.PlayerPrefabHash = uint.MinValue;
                callback.Approved = false;
                callback.Position = null;
                callback.Rotation = null;
                return;
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                callback.CreatePlayerObject = false;
                callback.PlayerPrefabHash = null;
                callback.Approved = true;
                callback.Position = null;
                callback.Rotation = null;
                return;
            }

            string payload = Encoding.UTF8.GetString(connectionData);
            ConnectionPayloadDto connectionPayload = JsonConverter.FromJson<ConnectionPayloadDto>(payload);

            ConnectStatus status = ConnectStatus.Success;

            if (gameInProgress)
                status = ConnectStatus.GameInProgress;
            else if (members.Count >= m_MaxPlayers)
                status = ConnectStatus.ServerFull;

            if (status == ConnectStatus.Success)
            {
                clientSceneMap[clientId] = connectionPayload.clientScene;
                clientIdToGuid[clientId] = connectionPayload.clientGUID;
                members[connectionPayload.clientGUID] =
                    new MemberData(SteamClient.SteamId, connectionPayload.displayName, clientId);
            }

            callback.CreatePlayerObject = false;
            callback.PlayerPrefabHash = 0;
            callback.Approved = true;
            callback.Position = null;
            callback.Rotation = null;

            portal.ServerToClientConnectResult(clientId, status);

            if (status != ConnectStatus.Success)
                StartCoroutine(WaitToDisconnectClient(clientId, status));
        }

        private IEnumerator WaitToDisconnectClient(ulong clientId, ConnectStatus reason, float seconds = 0f)
        {
            portal.ServerToClientSetDisconnectReason(clientId, reason);
            yield return new WaitForSeconds(seconds);
            KickClient(clientId);
        }

        #region Network Callbacks

        private void HandleNetworkReadied()
        {
            print("Network preparing");
            if (!NetworkManager.Singleton.IsServer)
                return;

            print("Network loaded successfully");
            SteamMultiplayerNetworkManager.OnClientDisconnectRequested += HandleUserDisconnectRequested;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            SteamMultiplayerNetworkManager.OnClientSceneChanged += HandleClientSceneChanged;

            Debug.Log("Network client callbacks registered");

            if (NetworkManager.Singleton.IsHost)
            {
                clientSceneMap[NetworkManager.Singleton.LocalClientId] = SceneManager.GetActiveScene().buildIndex;
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            clientSceneMap.Remove(clientId);

            if (clientIdToGuid.TryGetValue(clientId, out FixedString64Bytes guid))
            {
                clientIdToGuid.Remove(clientId);

                if (members[guid].ClientId == clientId)
                    members.Remove(guid);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                SteamMultiplayerNetworkManager.OnClientDisconnectRequested -= HandleUserDisconnectRequested;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
                SteamMultiplayerNetworkManager.OnClientSceneChanged -= HandleClientSceneChanged;
            }
        }

        private void HandleClientSceneChanged(ulong clientId, int sceneIndex) => clientSceneMap[clientId] = sceneIndex;

        private void HandleUserDisconnectRequested()
        {
            HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);

            NetworkManager.Singleton.Shutdown();

            ClearData();

            Debug.Log("Received disconnect request");
        }

        private void HandleServerStarted()
        {
            if (!NetworkManager.Singleton.IsHost)
                return;

            string clientGuid = Guid.NewGuid().ToString();
            string playerName = PlayerPrefs.GetString("PlayerName",
                SteamManager.Instance.IsConnectedToSteam
                    ? SteamClient.Name
                    : "Missing Name");

            members.TryAdd(clientGuid, new MemberData
            (
                SteamManager.Instance.IsConnectedToSteam
                    ? SteamClient.SteamId
                    : default,
                playerName,
                NetworkManager.Singleton.LocalClientId
            ));

            clientIdToGuid.TryAdd(NetworkManager.Singleton.LocalClientId, clientGuid);

            OnServerStarted?.Invoke();
        }

        #endregion
    }
}
