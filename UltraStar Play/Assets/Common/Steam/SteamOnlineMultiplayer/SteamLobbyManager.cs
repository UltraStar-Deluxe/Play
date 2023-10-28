using System;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    public class SteamLobbyManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static SteamLobbyManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamLobbyManager>();

        [Inject]
        private SteamManager steamManager;

        [Inject]
        private NetworkManager networkManager;

        public Lobby? CurrentLobby { get; private set; }

        private readonly Subject<LobbyEvent> lobbyEventStream = new();
        public IObservable<LobbyEvent> LobbyEventStream => lobbyEventStream;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
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
        }

        protected override void OnDestroySingleton()
        {
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyMemberKicked -= OnLobbyMemberKicked;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamMatchmaking.OnChatMessage -= OnChatMessage;
            SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        }

        private void OnApplicationQuit() => CurrentLobby?.Leave();

        public async Task<Lobby> CreateLobbyAsync(LobbyConfig config)
        {
            if (!steamManager.IsConnectedToSteam)
            {
                throw new OnlineMultiplayerException("Failed to create Steam lobby, not connected to Steam.");
            }

            Debug.Log("Creating new lobby");
            Lobby lobby = await SteamMatchmaking.CreateLobbyAsync(config.maxMembers)
                          ?? throw new OnlineMultiplayerException("Failed to create new lobby.");
            lobby.SetVisibility(config.visibility);
            lobby.SetJoinable(config.joinable);
            lobby.SetData("name", config.name);
            lobby.SetData("password", config.password);
            return lobby;
        }

        public async Task<Lobby[]> GetLobbiesAsync(string password)
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
            {
                Debug.LogWarning("Failed to find lobbies. Steam is not running");
                return Array.Empty<Lobby>();
            }

            LobbyQuery lobbyQuery = SteamMatchmaking.LobbyList
                .WithMaxResults(100);
            if (!password.IsNullOrEmpty())
            {
                lobbyQuery.WithKeyValue("password", password);
            }
            return await lobbyQuery.RequestAsync() ?? Array.Empty<Lobby>();
        }

        public void LeaveCurrentLobby()
        {
            CurrentLobby?.Leave();
            CurrentLobby = null;
        }

        private void FireLobbyEvent(LobbyEvent lobbyEvent)
        {
            lobbyEventStream.OnNext(lobbyEvent);
        }

        #region SteamCallbacks

        private void OnChatMessage(Lobby lobby, Friend friend, string message) =>
            FireLobbyEvent(new LobbyChatMessageReceivedEvent(lobby, friend, message));

        private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new LobbyDataChangedEvent(lobby));

        private void OnLobbyDataChanged(Lobby lobby) =>
            FireLobbyEvent(new LobbyDataChangedEvent(lobby));

        private void OnLobbyMemberJoined(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new MemberJoinedLobbyEvent(lobby, friend));

        private void OnLobbyMemberLeave(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new MemberLeftLobbyEvent(lobby, friend));

        private void OnLobbyMemberKicked(Lobby lobby, Friend friend, Friend user) =>
            FireLobbyEvent(new MemberKickedLobbyEvent(lobby, friend, user));

        private void OnLobbyMemberBanned(Lobby lobby, Friend friend, Friend user) =>
            FireLobbyEvent(new MemberBannedLobbyEvent(lobby, friend, user));

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new MemberLeftLobbyEvent(lobby, friend));

        private void OnLobbyInvite(Friend friend, Lobby lobby) =>
            FireLobbyEvent(new MemberInviteReceivedLobbyEvent(lobby, friend));

        private void OnLobbyEntered(Lobby lobby) =>
            FireLobbyEvent(new LobbyEnteredEvent(lobby));

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                Debug.LogError($"Lobby couldn't be created, {result}", this);
                return;
            }

            FireLobbyEvent(new LobbyCreatedEvent(lobby));
            FireLobbyEvent(new MemberJoinedLobbyEvent(lobby, lobby.Owner));
        }

        #endregion
    }
}
