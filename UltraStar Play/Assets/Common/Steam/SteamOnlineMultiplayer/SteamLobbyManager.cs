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
    public class SteamLobbyManager : AbstractSingletonBehaviour, INeedInjection, ILobbyManager
    {
        public static SteamLobbyManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamLobbyManager>();

        public const string EmptyPassword = "EMPTY_PASSWORD";

        [Inject]
        private SteamManager steamManager;

        [Inject]
        private NetworkManager networkManager;

        public ILobby CurrentLobby => CurrentSteamLobby;

        public SteamLobby currentSteamLobby;
        public SteamLobby CurrentSteamLobby
        {
            get => currentSteamLobby;
            private set
            {
                currentSteamLobby = value;
                if (value == null)
                {
                    Debug.Log($"Set CurrentLobby to null");
                }
                else
                {
                    Debug.Log($"Set CurrentLobby to lobby '{value?.Name}' with id {value?.Id}");
                }
            }
        }

        private readonly Subject<SteamLobbyEvent> lobbyEventStream = new();
        public IObservable<SteamLobbyEvent> LobbyEventStream => lobbyEventStream;

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

            if (CurrentSteamLobby != null)
            {
                LeaveCurrentLobby();
            }
        }

        public async Task<Lobby> CreateLobbyAsync(SteamLobbyConfig config)
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

            if (CurrentSteamLobby != null)
            {
                LeaveCurrentLobby();
            }

            Debug.Log($"Creating new lobby with config: {JsonConverter.ToJson(config)}");

            string passwordOrEmpty = GetPasswordOrEmptyPassword(config.password);

            Lobby lobby = await SteamMatchmaking.CreateLobbyAsync(config.maxMembers)
                          ?? throw new OnlineMultiplayerException("Failed to create new lobby.");
            lobby.SetVisibility(ESteamLobbyVisibility.Public);
            lobby.SetJoinable(config.joinable);
            lobby.SetName(config.name);
            lobby.SetPassword(passwordOrEmpty);

            CurrentSteamLobby = new SteamLobby(lobby);

            Debug.Log($"Successfully created new lobby with id {lobby.Id} and owner {lobby.Owner} and password {passwordOrEmpty} from config: {JsonConverter.ToJson(config)}");

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

            CurrentSteamLobby = new SteamLobby(lobby);

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

            string passwordOrEmpty = GetPasswordOrEmptyPassword(password);

            LobbyQuery lobbyQuery = SteamMatchmaking.LobbyList
                .FilterDistanceWorldwide()
                .WithMaxResults(100)
                .WithPassword(passwordOrEmpty);

            Lobby[] lobbies = await lobbyQuery.RequestAsync()
                                   ?? Array.Empty<Lobby>();

            Debug.Log($"Found {lobbies.Length} Steam lobbies. password: {passwordOrEmpty}");

            return lobbies;
        }

        public void LeaveCurrentLobby()
        {
            if (CurrentSteamLobby == null)
            {
                Debug.Log("Cannot leave Steam lobby because CurrentLobby is null");
                return;
            }

            try
            {
                Debug.Log($"Leaving Steam lobby '{CurrentSteamLobby?.Name}' with id {CurrentSteamLobby?.Id}");
                CurrentSteamLobby?.Leave();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to leave lobby: {ex.Message}");
            }
            finally
            {
                CurrentSteamLobby = null;
            }

            try
            {
                if (networkManager.IsConnectedClient)
                {
                    networkManager.ShutdownIfConnectedClient("Leaving Steam lobby");
                }
                else
                {
                    Debug.Log("Leaving Steam lobby, but not connected as Netcode client. Thus, not shutting down NetworkManager.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void FireLobbyEvent(SteamLobbyEvent steamLobbyEvent)
        {
            Debug.Log($"FireLobbyEvent: {steamLobbyEvent}");
            lobbyEventStream.OnNext(steamLobbyEvent);
        }

        #region SteamCallbacks

        private void OnChatMessage(Lobby lobby, Friend friend, string message) =>
            FireLobbyEvent(new SteamLobbyChatMessageReceivedEvent(lobby, friend, message));

        private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new SteamLobbyDataChangedEvent(lobby));

        private void OnLobbyDataChanged(Lobby lobby) =>
            FireLobbyEvent(new SteamLobbyDataChangedEvent(lobby));

        private void OnLobbyMemberJoined(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new MemberJoinedSteamLobbyEvent(lobby, friend));

        private void OnLobbyMemberLeave(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new MemberLeftSteamLobbyEvent(lobby, friend));

        private void OnLobbyMemberKicked(Lobby lobby, Friend friend, Friend user) =>
            FireLobbyEvent(new MemberKickedSteamLobbyEvent(lobby, friend, user));

        private void OnLobbyMemberBanned(Lobby lobby, Friend friend, Friend user) =>
            FireLobbyEvent(new MemberBannedSteamLobbyEvent(lobby, friend, user));

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend) =>
            FireLobbyEvent(new MemberLeftSteamLobbyEvent(lobby, friend));

        private void OnLobbyInvite(Friend friend, Lobby lobby) =>
            FireLobbyEvent(new MemberInviteReceivedSteamLobbyEvent(lobby, friend));

        private void OnLobbyEntered(Lobby lobby) =>
            FireLobbyEvent(new SteamLobbyEnteredEvent(lobby));

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                Debug.LogError($"Lobby couldn't be created, {result}", this);
                return;
            }

            FireLobbyEvent(new SteamLobbyCreatedEvent(lobby));
            FireLobbyEvent(new MemberJoinedSteamLobbyEvent(lobby, lobby.Owner));
        }

        #endregion

        public static bool IsNonEmptyPassword(string lobbyPassword)
        {
            return !lobbyPassword.IsNullOrEmpty() && lobbyPassword != EmptyPassword;
        }

        private static string GetPasswordOrEmptyPassword(string password)
        {
            // Steam does not list lobbies when password is set to the empty string, even when searching for this.
            // As workaround, the empty string is replaced here.
            return password.IsNullOrEmpty() ? EmptyPassword : password;
        }
    }
}
