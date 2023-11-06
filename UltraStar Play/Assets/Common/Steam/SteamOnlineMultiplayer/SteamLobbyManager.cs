using System;
using System.Threading.Tasks;
using CommonOnlineMultiplayer;
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

        public Lobby? currentLobby;
        public Lobby? CurrentLobby
        {
            get => currentLobby;
            private set
            {
                currentLobby = value;
                if (value == null)
                {
                    Debug.Log($"Set CurrentLobby to null");
                }
                else
                {
                    Debug.Log($"Set CurrentLobby to lobby '{value?.GetName()}' with id {value?.Id}");
                }
            }
        }

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

            if (CurrentLobby != null)
            {
                LeaveCurrentLobby();
            }
        }

        public async Task<Lobby> CreateLobbyAsync(LobbyConfig config)
        {
            if (!steamManager.IsConnectedToSteam)
            {
                throw new OnlineMultiplayerException("Failed to create lobby, not connected to Steam.");
            }

            if (config.name.IsNullOrEmpty())
            {
                throw new OnlineMultiplayerException("Missing name for lobby.");
            }

            if (config.visibility is not ESteamLobbyVisibility.Public
                && config.password.IsNullOrEmpty())
            {
                throw new OnlineMultiplayerException("Missing password for hidden lobby.");
            }

            if (CurrentLobby != null)
            {
                LeaveCurrentLobby();
            }

            Debug.Log($"Creating new lobby with config: {JsonConverter.ToJson(config)}");

            Lobby lobby = await SteamMatchmaking.CreateLobbyAsync(config.maxMembers)
                          ?? throw new OnlineMultiplayerException("Failed to create new lobby.");
            lobby.SetVisibility(config.visibility);
            lobby.SetJoinable(config.joinable);
            lobby.SetName(config.name);
            lobby.SetPassword(config.password);

            CurrentLobby = lobby;

            Debug.Log($"Successfully created new lobby with id {lobby.Id} and owner {lobby.Owner} from config: {JsonConverter.ToJson(config)}");

            return lobby;
        }

        public async Task<Lobby> JoinLobbyAsync(Lobby lobby)
        {
            if (!steamManager.IsConnectedToSteam)
            {
                throw new OnlineMultiplayerException("Failed to join lobby, not connected to Steam");
            }

            Debug.Log($"Joining lobby '{lobby.GetName()}' with id {lobby.Id}");
            RoomEnter roomEnter = await lobby.Join();
            if (roomEnter is not RoomEnter.Success)
            {
                throw new OnlineMultiplayerException($"Failed to join lobby '{lobby.GetName()}' with {lobby.Id}: {roomEnter}");
            }

            CurrentLobby = lobby;

            Debug.Log($"Successfully joined lobby '{lobby.GetName()}' with id {lobby.Id} and owner {lobby.Owner}");
            return lobby;
        }

        public async Task<Lobby[]> GetLobbiesAsync(string password)
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
            {
                Debug.LogError("Failed to find lobbies. Steam is not running");
                return Array.Empty<Lobby>();
            }

            LobbyQuery lobbyQuery = SteamMatchmaking.LobbyList
                .WithMaxResults(100);
            if (!password.IsNullOrEmpty())
            {
                lobbyQuery.WithPassword(password);
            }
            return await lobbyQuery.RequestAsync()
                   ?? Array.Empty<Lobby>();
        }

        public void LeaveCurrentLobby()
        {
            if (CurrentLobby == null)
            {
                Debug.Log("Cannot leave Steam lobby because CurrentLobby is null");
                return;
            }

            try
            {
                Debug.Log($"Leaving Steam lobby '{CurrentLobby?.GetName()}' with id {CurrentLobby?.Id}");
                CurrentLobby?.Leave();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to leave lobby: {ex.Message}");
            }
            finally
            {
                CurrentLobby = null;
            }
        }

        private void FireLobbyEvent(LobbyEvent lobbyEvent)
        {
            Debug.Log($"FireLobbyEvent: {lobbyEvent}");
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
