using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonOnlineMultiplayer;
using Netcode.Transports.Facepunch;
using SteamOnlineMultiplayer.RequestHandlers;
using Steamworks;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SteamOnlineMultiplayer
{
    public class SteamMultiplayerManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static SteamMultiplayerManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamMultiplayerManager>();

        private const int MaxConnectionDataLength = 2048;

        private readonly SteamLobbyMemberRegistry steamLobbyMemberRegistry = new();

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

        private readonly Subject<SteamLobbyMemberChangedEvent> connectedMemberDataChangedEventSteam = new();
        public IObservable<SteamLobbyMemberChangedEvent> ConnectedMemberDataChangedEventSteam => connectedMemberDataChangedEventSteam
            .ObserveOnMainThread();

        private readonly Subject<AbstractLobbyMemberConnectionChangedEvent> networkClientConnectionChangedEventSteam = new();
        public IObservable<AbstractLobbyMemberConnectionChangedEvent> NetworkClientConnectionChangedEventSteam => networkClientConnectionChangedEventSteam
            .ObserveOnMainThread();

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

            RegisterNetcodeRequestHandlers();
        }

        private void RegisterNetcodeRequestHandlers()
        {
            NetcodeRequestHandlerRegistry.Instance.AddRequestHandler(new CurrentSteamLobbyMembersRequestHandler());
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
            Debug.Log($"OnUnityNetcodeClientStopped(wasHostMode: {wasHostMode})");
            UiManager.CreateNotification("Disconnected from online game");

            if (lobbyManager.CurrentLobby != null)
            {
                lobbyManager.LeaveCurrentLobby();
            }

            if (wasHostMode)
            {
                IReadOnlyList<SteamLobbyMember> memberDatas = steamLobbyMemberRegistry.GetAllData();
                steamLobbyMemberRegistry.Clear();
                memberDatas.ForEach(memberData => connectedMemberDataChangedEventSteam.OnNext(new SteamLobbyMemberRemovedEvent(memberData)));
            }
        }

        private void OnNetcodeServerStopped(bool wasHostMode)
        {
            Debug.Log($"OnUnityNetcodeServerStopped(wasHostMode: {wasHostMode})");

            if (lobbyManager.CurrentLobby != null)
            {
                lobbyManager.LeaveCurrentLobby();
            }

            if (wasHostMode)
            {
                IReadOnlyList<SteamLobbyMember> memberDatas = steamLobbyMemberRegistry.GetAllData();
                steamLobbyMemberRegistry.Clear();
                memberDatas.ForEach(memberData => connectedMemberDataChangedEventSteam.OnNext(new SteamLobbyMemberRemovedEvent(memberData)));
            }
        }

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

            SteamLobbyConnectionRequestDto requestDto = new(
                steamManager.PlayerName,
                steamManager.PlayerSteamId.Value);
            string payload = requestDto.ToJson();
            networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
            bool success = networkManager.StartClient();
            if (!success)
            {
                throw new OnlineMultiplayerException("Failed to start Unity Netcode client");
            }

            Debug.Log("Successfully started Unity Netcode client");
        }

        public SteamLobbyMember? GetMemberDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId)
        {
            if (steamLobbyMemberRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out SteamLobbyMember memberData))
            {
                    return memberData;
            }

            Debug.LogWarning($"No member data found for net client id: {netcodeClientId}");
            return null;
        }

        public IReadOnlyList<SteamLobbyMember> GetMembers()
        {
            return steamLobbyMemberRegistry.GetAllData();
        }

        public SteamLobbyMember? GetMemberDataBySteamId(SteamId steamId)
        {
            if (steamLobbyMemberRegistry.TryGetDataBySteamId(steamId, out SteamLobbyMember memberData))
            {
                return memberData;
            }

            Debug.LogWarning($"No member data found for Steam id: {steamId}");
            return null;
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
            networkClientConnectionChangedEventSteam.OnNext(new LobbyMemberConnectedEvent(netcodeClientId));
        }

        private void OnNetcodeClientDisconnected(ulong netcodeClientId)
        {
            Debug.Log($"OnClientDisconnected(UnityNetcodeClientId: {netcodeClientId})");
            if (steamLobbyMemberRegistry.TryGetDataByUnityNetcodeClientId(netcodeClientId, out SteamLobbyMember memberData))
            {
                steamLobbyMemberRegistry.Remove(memberData);
                connectedMemberDataChangedEventSteam.OnNext(new SteamLobbyMemberRemovedEvent(memberData));
            }
            networkClientConnectionChangedEventSteam.OnNext(new LobbyMemberDisconnectedEvent(netcodeClientId));
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

            void ApproveRequest(SteamLobbyMember memberData, string reason)
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

                steamLobbyMemberRegistry.Add(memberData);
                Debug.Log($"Added member data: {JsonConverter.ToJson(memberData)}");
                connectedMemberDataChangedEventSteam.OnNext(new SteamLobbyMemberAddedEvent(memberData));
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
                SteamLobbyMember ownSteamLobbyMember = new SteamLobbyMember(
                    netcodeClientId,
                    steamManager.PlayerName,
                    steamManager.PlayerSteamId);
                ApproveRequest(ownSteamLobbyMember, "approval request from ourself");
                return;
            }

            string payload = Encoding.UTF8.GetString(connectionData, 0, Math.Min(connectionData.Length, MaxConnectionDataLength));
            Debug.Log($"Connection payload: {payload}");
            SteamLobbyConnectionRequestDto requestDto;
            try
            {
                requestDto = JsonConverter.FromJson<SteamLobbyConnectionRequestDto>(payload, false);
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

            SteamLobbyMember steamLobbyMember = new SteamLobbyMember(
                netcodeClientId,
                requestDto.DisplayName,
                requestDto.SteamId);
            ApproveRequest(steamLobbyMember, $"payload is OK");
        }

        private bool IsConnectionRequestDataValid(SteamLobbyConnectionRequestDto requestDto, out string errorMessage)
        {
            if (requestDto.SteamId <= 0)
            {
                errorMessage = "SteamId is missing";
                return false;
            }

            if (requestDto.DisplayName.IsNullOrEmpty())
            {
                errorMessage = "DisplayName is missing";
                return false;
            }

            errorMessage = "";
            return true;
        }

        #endregion
    }
}
