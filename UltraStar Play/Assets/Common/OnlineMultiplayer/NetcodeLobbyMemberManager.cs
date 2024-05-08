using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniInject;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CommonOnlineMultiplayer
{
    public class NetcodeLobbyMemberManager : AbstractSingletonBehaviour, INeedInjection, ILobbyMemberManager
    {
        public static NetcodeLobbyMemberManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<NetcodeLobbyMemberManager>();

        private const int MaxConnectionDataLength = 2048;

        private readonly LobbyMemberRegistry lobbyMemberRegistry = new();

        [Inject]
        private NetworkManager networkManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        public void OnNetcodeClientConnectionApproval(
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

            void ApproveRequest(LobbyMember lobbyMember, string reason)
            {
                Debug.Log($"ApproveRequest: {reason}");

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

                lobbyMemberRegistry.Add(lobbyMember);
                Debug.Log($"Added member data: {JsonConverter.ToJson(lobbyMember)}");
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
                LobbyMember ownLobbyMember = new LobbyMember(
                    netcodeClientId,
                    "HostPlayer");
                ApproveRequest(ownLobbyMember, "approval request from ourself");
                return;
            }

            string payload = Encoding.UTF8.GetString(connectionData, 0, Math.Min(connectionData.Length, MaxConnectionDataLength));
            Debug.Log($"Connection payload: {payload}");
            LobbyConnectionRequestDto requestDto;
            try
            {
                requestDto = JsonConverter.FromJson<LobbyConnectionRequestDto>(payload);
                if (!IsConnectionRequestDataValid(requestDto, out string errorMessage))
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

            LobbyMember lobbyMember = new LobbyMember(
                netcodeClientId,
                requestDto.DisplayName);
            ApproveRequest(lobbyMember, $"payload is OK");
        }

        public LobbyMember GetLobbyMember(UnityNetcodeClientId netcodeClientId)
        {
            if (networkManager.IsServer)
            {
                if (lobbyMemberRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out LobbyMember lobbyMember))
                {
                    return lobbyMember;
                }
                Debug.LogWarning($"No lobby member found for Netcode client id: {netcodeClientId}");
                return null;
            }
            else if (networkManager.IsClient)
            {
                return GetLobbyMembers().FirstOrDefault(it => it.UnityNetcodeClientId == netcodeClientId);
            }

            return null;
        }

        private bool IsConnectionRequestDataValid(LobbyConnectionRequestDto requestDto, out string errorMessage)
        {
            if (requestDto.DisplayName.IsNullOrEmpty())
            {
                errorMessage = "DisplayName is missing";
                return false;
            }

            errorMessage = "";
            return true;
        }

        public void RemoveLobbyMemberFromRegistry(UnityNetcodeClientId netcodeClientId)
        {
            if (lobbyMemberRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out LobbyMember lobbyMember))
            {
                lobbyMemberRegistry.Remove(lobbyMember);
            }
        }

        public void ClearLobbyMemberRegistry()
        {
            lobbyMemberRegistry.Clear();
        }

        public void UpdateLobbyMemberRegistry()
        {
            if (networkManager.IsServer)
            {
                // Registry of server is updated in approval request
                return;
            }

            ClearLobbyMemberRegistry();
            GetLobbyMembersFromSpawnedNetworkObjects()
                .ForEach(steamLobbyMember => lobbyMemberRegistry.Add(steamLobbyMember));
        }

        private List<LobbyMember> GetLobbyMembersFromSpawnedNetworkObjects()
        {
            List<LobbyMember> result = new();
            if (networkManager == null
                || networkManager.SpawnManager == null
                || networkManager.SpawnManager.SpawnedObjectsList.IsNullOrEmpty())
            {
                return result;
            }

            networkManager.SpawnManager.SpawnedObjectsList.ForEach(networkObject =>
            {
                LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = networkObject.GetComponent<LobbyMemberNetworkBehaviour>();
                if (lobbyMemberNetworkBehaviour == null
                    || lobbyMemberNetworkBehaviour.LobbyMemberJson.IsNullOrEmpty())
                {
                    return;
                }

                result.Add(lobbyMemberNetworkBehaviour.LobbyMember);
            });
            return result;
        }

        public IReadOnlyList<LobbyMember> GetLobbyMembers()
        {
            return lobbyMemberRegistry.GetAllLobbyMembers();
        }
    }
}
