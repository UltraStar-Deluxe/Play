using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonOnlineMultiplayer;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SteamOnlineMultiplayer
{
    public class SteamMultiplayerManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static SteamMultiplayerManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamMultiplayerManager>();

        private const int MaxConnectionDataLength = 2048;

        private ConnectedMemberDataRegistry connectedMemberDataRegistry = new();

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private SteamManager steamManager;

        [Inject]
        private SceneNavigator sceneNavigator;

        [Inject]
        private SteamLobbyManager lobbyManager;

        [Inject]
        private FacepunchTransport injectedFacepunchTransport;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.ConnectionApprovalCallback += OnClientConnectionApproval;

            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnServerStopped += OnServerStopped;

            networkManager.OnClientStarted += OnClientStarted;
            networkManager.OnClientStopped += OnClientStopped;

            sceneNavigator.SceneChangedEventStream
                .Subscribe(evt => OnClientSceneChanged(networkManager.LocalClientId, (int)evt.NewScene))
                .AddTo(gameObject);
        }

        protected override void OnDestroySingleton()
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.ConnectionApprovalCallback -= OnClientConnectionApproval;

            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnServerStopped -= OnServerStopped;

            networkManager.OnClientStarted -= OnClientStarted;
            networkManager.OnClientStopped -= OnClientStopped;
        }

        public async Task CreateLobbyAsync(LobbyConfig lobbyConfig)
        {
            if (!steamManager.IsConnectedToSteam)
            {
                throw new OnlineMultiplayerException("Failed to host game, not connected to Steam");
            }

            await lobbyManager.CreateLobbyAsync(lobbyConfig);
        }

        public void RequestDisconnect()
        {
            if (lobbyManager.CurrentLobby == null)
            {
                throw new OnlineMultiplayerException("Cannot disconnect, not yet connected to a lobby");
            }

            if (NetworkManager.Singleton.IsServer)
                KickAllClients();

            networkManager.DisconnectClient(NetworkManager.Singleton.LocalClientId, "CLIENT REQUESTED DISCONNECT");

            void KickAllClients()
            {
                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.ClientId.Equals(NetworkManager.Singleton.LocalClient.ClientId))
                    {
                        continue;
                    }

                    networkManager.DisconnectClient(client.ClientId, "SERVER REQUESTED DISCONNECT");
                }
            }
        }

        /**
         * Can be used to join a lobby via a sharable code.
         */
        public void JoinLobbyByCode(string lobbyCode)
        {
            if (!steamManager.IsConnectedToSteam)
            {
                throw new OnlineMultiplayerException("Failed to join lobby, not connected to Steam");
            }

            string hexCode = lobbyCode
                .Replace("-", string.Empty)
                .Trim();
            SteamId lobbyId = Convert.ToUInt64(hexCode, fromBase: 16);
            // TODO: Find lobby with matching ID, join if such a lobby has been found.
        }

        public void JoinLobby(Lobby lobby)
        {
            Debug.Log($"Joining lobby {lobby.Id} that is hosted by {lobby.Owner.Id}");
            UpdateFacepunchTransport(lobby.Owner.Id);
            StartNetcodeNetworkManagerClient();
        }

        private void UpdateFacepunchTransport(SteamId targetSteamId)
        {
            if (networkManager.NetworkConfig.NetworkTransport is not FacepunchTransport)
            {
                networkManager.NetworkConfig.NetworkTransport = injectedFacepunchTransport;
            }

            FacepunchTransport configuredFacepunchTransport = networkManager.NetworkConfig.NetworkTransport as FacepunchTransport;
            if (configuredFacepunchTransport.targetSteamId != targetSteamId)
            {
                Debug.Log($"Set FacepunchTransport.targetSteamId to {targetSteamId}");
                configuredFacepunchTransport.targetSteamId = targetSteamId;
            }
        }

        public void StartNetcodeNetworkManagerHost()
        {
            bool success = networkManager.StartHost();
            if (!success)
            {
                throw new OnlineMultiplayerException("Failed to start Unity Netcode host");
            }

            Debug.Log("Successfully started Unity Netcode host");
        }

        private void StartNetcodeNetworkManagerClient()
        {
            RemotePlayerConnectionDataDto connectionData = new(
                networkManager.LocalClientId,
                steamManager.PlayerSteamId.Value,
                Guid.NewGuid().ToString(),
                SteamManager.Instance.PlayerName,
                SceneManager.GetActiveScene().name);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(connectionData.ToJson());

            networkManager.NetworkConfig.ConnectionData = payloadBytes;
            bool success = networkManager.StartClient();
            if (!success)
            {
                throw new OnlineMultiplayerException("Failed to start Unity Netcode client");
            }

            Debug.Log("Successfully started Unity Netcode client");
        }

        public MemberData? GetMemberDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId)
        {
            if (connectedMemberDataRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out MemberData memberData))
            {
                    return memberData;
            }

            Debug.LogWarning($"No member data found for net client id: {netcodeClientId}");
            return null;
        }

        public IReadOnlyList<MemberData> GetMembers()
        {
            return connectedMemberDataRegistry.GetAllData();
        }

        public MemberData? GetMemberDataBySteamId(SteamId steamId)
        {
            if (connectedMemberDataRegistry.TryGetDataBySteamId(steamId, out MemberData memberData))
            {
                return memberData;
            }

            Debug.LogWarning($"No member data found for Steam id: {steamId}");
            return null;
        }

        private void KickClient(UnityNetcodeClientId netcodeClientId)
        {
            if (!networkManager.IsServer)
                return;

            if (networkManager.SpawnManager != null)
            {
                NetworkObject networkObject = networkManager.SpawnManager.GetPlayerNetworkObject(netcodeClientId);

                if (networkObject != null)
                    networkObject.Despawn(true);
            }

            networkManager.DisconnectClient(netcodeClientId);
        }

        private void OnClientSceneChanged(UnityNetcodeClientId netcodeClientId, int sceneIndex)
        {
            if (connectedMemberDataRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out MemberData memberData))
            {
                try
                {
                    memberData.CurrentSceneName = SceneManager.GetSceneAt(sceneIndex).name;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to update current scene name of remote player '{memberData.DisplayName}'");
                }
            }
        }

        public List<Friend> GetFriends()
        {
            if (!steamManager.IsConnectedToSteam)
                return new List<Friend>();

            return SteamFriends
                .GetFriends()
                .ToList();
        }

        #region SteamCallbacks

        private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
        {
            Debug.Log($"Received request to join lobby hosted by {steamId}");
            JoinLobby(lobby);
        }

        #endregion

        #region NetcodeNetworkManagerCallbacks

        private void OnClientConnected(ulong netcodeClientId)
        {
            Debug.Log("Network client callbacks registered");
        }

        private void OnClientDisconnected(ulong netcodeClientId)
        {
            if (connectedMemberDataRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out MemberData memberData))
            {
                connectedMemberDataRegistry.Remove(memberData);
            }
        }

        private void OnServerStarted()
        {
            if (!networkManager.IsHost)
                return;

            MemberData ownMemberData = new MemberData
            (
                networkManager.LocalClientId,
                SteamManager.Instance.PlayerSteamId,
                Guid.NewGuid().ToString(),
                SteamManager.Instance.PlayerName,
                SceneManager.GetActiveScene().name
            );

            connectedMemberDataRegistry.Add(ownMemberData);

        }

        private void OnServerStopped(bool isHostInsteadOfServer)
        {

        }

        private void OnClientStarted()
        {

        }

        private void OnClientStopped(bool obj)
        {
        }

        private void OnClientConnectionApproval(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest,
            NetworkManager.ConnectionApprovalResponse callback)
        {
            void DenyRequest()
            {
                callback.CreatePlayerObject = false;
                callback.PlayerPrefabHash = uint.MinValue;
                callback.Approved = false;
                callback.Position = null;
                callback.Rotation = null;
            }

            void ApproveRequest()
            {
                callback.CreatePlayerObject = false;
                callback.PlayerPrefabHash = 0;
                callback.Approved = true;
                callback.Position = null;
                callback.Rotation = null;
            }

            byte[] connectionData = connectionApprovalRequest.Payload;
            UnityNetcodeClientId netcodeClientId = connectionApprovalRequest.ClientNetworkId;
            if (connectionData.Length > MaxConnectionDataLength)
            {
                DenyRequest();
                return;
            }

            if (netcodeClientId == networkManager.LocalClientId)
            {
                // This a request from ourself
                ApproveRequest();
                return;
            }

            string payload = Encoding.UTF8.GetString(connectionData);
            RemotePlayerConnectionDataDto connectionDataDto = JsonConverter.FromJson<RemotePlayerConnectionDataDto>(payload);

            if (connectionDataDto.UnityNetcodeClientId != netcodeClientId)
            {
                // clientId of Unity API object does not match clientId of JSON payload
                DenyRequest();
                return;
            }

            MemberData memberData = new MemberData(
                connectionDataDto.UnityNetcodeClientId,
                connectionDataDto.SteamId,
                connectionDataDto.ConnectionGuid,
                connectionDataDto.DisplayName,
                connectionDataDto.CurrentSceneName);
            connectedMemberDataRegistry.Add(memberData);

            ApproveRequest();
        }

        #endregion
    }
}
