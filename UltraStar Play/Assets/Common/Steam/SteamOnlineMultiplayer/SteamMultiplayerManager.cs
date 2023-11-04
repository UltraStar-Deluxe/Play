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
using Random = UnityEngine.Random;

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
            networkManager.OnClientConnectedCallback += OnNetcodeClientConnected;
            networkManager.OnClientDisconnectCallback += OnNetcodeClientDisconnected;
            networkManager.ConnectionApprovalCallback += OnNetcodeClientConnectionApproval;
            networkManager.OnClientStopped += OnNetcodeClientStopped;
            networkManager.OnServerStopped += OnNetcodeServerStopped;

            sceneNavigator.SceneChangedEventStream
                .Subscribe(evt => OnClientSceneChanged(networkManager.LocalClientId, (int)evt.NewScene))
                .AddTo(gameObject);
        }

        protected override void OnDestroySingleton()
        {
            networkManager.OnClientConnectedCallback -= OnNetcodeClientConnected;
            networkManager.OnClientDisconnectCallback -= OnNetcodeClientDisconnected;
            networkManager.ConnectionApprovalCallback -= OnNetcodeClientConnectionApproval;
            networkManager.OnClientStopped -= OnNetcodeClientStopped;
            networkManager.OnServerStopped -= OnNetcodeServerStopped;
        }

        private void OnNetcodeClientStopped(bool wasHostMode)
        {
            Debug.Log("OnUnityNetcodeClientStopped");
            UiManager.CreateNotification("Disconnected from online game");
            if (lobbyManager.CurrentLobby != null)
            {
                lobbyManager.LeaveCurrentLobby();
            }
        }

        private void OnNetcodeServerStopped(bool wasHostMode)
        {
            Debug.Log("OnUnityNetcodeServerStopped");
            if (lobbyManager.CurrentLobby != null)
            {
                lobbyManager.LeaveCurrentLobby();
            }
        }

        // /**
        //  * Can be used to join a lobby via a sharable code.
        //  */
        // public void JoinLobbyByCode(string lobbyCode)
        // {
        //     if (!steamManager.IsConnectedToSteam)
        //     {
        //         throw new OnlineMultiplayerException("Failed to join lobby, not connected to Steam");
        //     }
        //
        //     string hexCode = lobbyCode
        //         .Replace("-", string.Empty)
        //         .Trim();
        //     SteamId lobbyId = Convert.ToUInt64(hexCode, fromBase: 16);
        //     // TODO: Find lobby with matching ID, join if such a lobby has been found.
        // }

        private void ConfigureFacepunchTransport(SteamId targetSteamId)
        {
            if (networkManager.NetworkConfig.NetworkTransport is not FacepunchTransport)
            {
                networkManager.NetworkConfig.NetworkTransport = injectedFacepunchTransport;
            }

            FacepunchTransport configuredFacepunchTransport = networkManager.NetworkConfig.NetworkTransport as FacepunchTransport;
            if (configuredFacepunchTransport != null)
            {
                Debug.Log($"Set FacepunchTransport.targetSteamId to {targetSteamId}");
                configuredFacepunchTransport.targetSteamId = targetSteamId;
            }
        }

        public void StartNetcodeNetworkManagerHost()
        {
            ConfigureFacepunchTransport(0);

            bool success = networkManager.StartHost();
            if (!success)
            {
                throw new OnlineMultiplayerException("Failed to start Unity Netcode host");
            }

            Debug.Log("Successfully started Unity Netcode host");
        }

        public void StartNetcodeNetworkManagerClient(SteamId targetSteamId)
        {
            ConfigureFacepunchTransport(targetSteamId);

            NetworkPlayerConnectionRequestDataDto requestDataDto = new(
                steamManager.PlayerSteamId.Value,
                Guid.NewGuid().ToString(),
                steamManager.PlayerName,
                SceneManager.GetActiveScene().name);
            string payload = requestDataDto.ToJson();
            networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
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
            Debug.Log($"OnClientSceneChanged(UnityNetcodeClientId: {netcodeClientId}, sceneIndex: {sceneIndex})");

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
            Debug.Log($"Received request to join lobby '{lobby.GetName()}' with id {lobby.Id} owned by {steamId}");
        }

        #endregion

        #region NetcodeNetworkManagerCallbacks

        private void OnNetcodeClientConnected(ulong netcodeClientId)
        {
            Debug.Log($"OnClientConnected(UnityNetcodeClientId: {netcodeClientId})");
        }

        private void OnNetcodeClientDisconnected(ulong netcodeClientId)
        {
            Debug.Log($"OnClientDisconnected(UnityNetcodeClientId: {netcodeClientId})");
            if (connectedMemberDataRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out MemberData memberData))
            {
                connectedMemberDataRegistry.Remove(memberData);
            }
        }

        private void OnNetcodeClientConnectionApproval(
            NetworkManager.ConnectionApprovalRequest connectionApprovalRequest,
            NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log($"Checking approval of connection request (UnityNetcodeClientId: {connectionApprovalRequest.ClientNetworkId})");

            void DenyRequest()
            {
                Debug.Log("DenyRequest");
                response.Approved = false;
                response.CreatePlayerObject = false;
            }

            void ApproveRequest()
            {
                Debug.Log("ApproveRequest");
                // Your approval logic determines the following values
                response.Approved = true;
                response.CreatePlayerObject = true;

                // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
                response.PlayerPrefabHash = null;

                // Position to spawn the player object (if null it uses default of Vector3.zero)
                response.Position = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));

                // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
                response.Rotation = Quaternion.Euler(Random.Range(0,359), Random.Range(0,359), Random.Range(0,359));

                // If additional approval steps are needed, set this to true until the additional steps are complete
                // once it transitions from true to false the connection approval response will be processed.
                response.Pending = false;
            }

            byte[] connectionData = connectionApprovalRequest.Payload;
            UnityNetcodeClientId netcodeClientId = connectionApprovalRequest.ClientNetworkId;
            if (connectionData.Length > MaxConnectionDataLength)
            {
                Debug.Log($"DenyRequest because connection payload is longer than {MaxConnectionDataLength} bytes");
                DenyRequest();
                return;
            }

            if (netcodeClientId == networkManager.LocalClientId)
            {
                // This a request from ourself
                Debug.Log("ApproveRequest from ourself");
                ApproveRequest();
                return;
            }

            string payload = Encoding.UTF8.GetString(connectionData, 0, Math.Min(connectionData.Length, MaxConnectionDataLength));
            Debug.Log($"Connection payload: {payload}");
            NetworkPlayerConnectionRequestDataDto requestDataDto;
            try
            {
                requestDataDto = JsonConverter.FromJson<NetworkPlayerConnectionRequestDataDto>(payload, false);
                if (!IsConnectionRequestDataValid(requestDataDto, out string errorMessage))
                {
                    Debug.Log($"DenyRequest because connection data is invalid: {errorMessage}");
                    DenyRequest();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"DenyRequest because payload could not be deserialized: {ex.Message}");
                DenyRequest();
                return;
            }

            MemberData memberData = new MemberData(
                netcodeClientId,
                requestDataDto.SteamId,
                requestDataDto.ConnectionGuid,
                requestDataDto.DisplayName,
                requestDataDto.CurrentSceneName);
            connectedMemberDataRegistry.Add(memberData);

            Debug.Log($"ApproveRequest because payload is OK. Added member data: {JsonConverter.ToJson(memberData)}");
            ApproveRequest();
        }

        private bool IsConnectionRequestDataValid(NetworkPlayerConnectionRequestDataDto requestDataDto, out string errorMessage)
        {
            if (requestDataDto.SteamId <= 0)
            {
                errorMessage = "SteamId is missing";
                return false;
            }

            if (requestDataDto.DisplayName.IsNullOrEmpty())
            {
                errorMessage = "DisplayName is missing";
                return false;
            }

            if (requestDataDto.ConnectionGuid.IsNullOrEmpty())
            {
                errorMessage = "ConnectionGuid is missing";
                return false;
            }

            if (requestDataDto.CurrentSceneName.IsNullOrEmpty())
            {
                errorMessage = "CurrentSceneName is missing";
                return false;
            }

            errorMessage = "";
            return true;
        }

        #endregion
    }
}
