using Steamworks;
using Steamworks.Data;

namespace SteamOnlineMultiplayer
{
    public class SteamLobbyEvent
    {
        public Lobby Lobby { get; private set; }

        public SteamLobbyEvent(Lobby lobby)
        {
            Lobby = lobby;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id})";
        }
    }

    public class SteamLobbyCreatedEvent : SteamLobbyEvent
    {
        public SteamLobbyCreatedEvent(Lobby lobby) : base(lobby)
        {
        }
    }

    public class SteamLobbyDataChangedEvent : SteamLobbyEvent
    {
        public SteamLobbyDataChangedEvent(Lobby lobby) : base(lobby)
        {
        }
    }

    public class SteamLobbyEnteredEvent : SteamLobbyEvent
    {
        public SteamLobbyEnteredEvent(Lobby lobby) : base(lobby)
        {
        }
    }

    public class HasFriendSteamLobbyEvent : SteamLobbyEvent
    {
        public Friend Friend { get; set; }

        public HasFriendSteamLobbyEvent(Lobby lobby, Friend friend) : base(lobby)
        {
            Friend = friend;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, Friend: {Friend})";
        }
    }

    public class MemberJoinedSteamLobbyEvent : HasFriendSteamLobbyEvent
    {
        public MemberJoinedSteamLobbyEvent(Lobby lobby, Friend friend) : base(lobby, friend)
        {
        }
    }

    public class MemberLeftSteamLobbyEvent : HasFriendSteamLobbyEvent
    {
        public MemberLeftSteamLobbyEvent(Lobby lobby, Friend friend) : base(lobby, friend)
        {
        }
    }

    public class MemberInviteReceivedSteamLobbyEvent : HasFriendSteamLobbyEvent
    {
        public MemberInviteReceivedSteamLobbyEvent(Lobby lobby, Friend friend) : base(lobby, friend)
        {
        }
    }

    public class MemberKickedSteamLobbyEvent : SteamLobbyEvent
    {
        public Friend ExecutingMember { get; private set; }
        public Friend KickedMember { get; private set; }

        public MemberKickedSteamLobbyEvent(Lobby lobby, Friend executingMember, Friend kickedMember) : base(lobby)
        {
            ExecutingMember = executingMember;
            KickedMember = kickedMember;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, ExecutingMember: {ExecutingMember}, KickedMember: {KickedMember})";
        }
    }

    public class MemberBannedSteamLobbyEvent : SteamLobbyEvent
    {
        public Friend ExecutingMember { get; private set; }
        public Friend BannedMember { get; private set; }

        public MemberBannedSteamLobbyEvent(Lobby lobby, Friend executingMember, Friend bannedMember) : base(lobby)
        {
            ExecutingMember = executingMember;
            BannedMember = bannedMember;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, ExecutingMember: {ExecutingMember}, BannedMember: {BannedMember})";
        }
    }

    public class SteamLobbyChatMessageReceivedEvent : HasFriendSteamLobbyEvent
    {
        public string Message { get; private set; }

        public SteamLobbyChatMessageReceivedEvent(Lobby lobby, Friend friend, string message) : base(lobby, friend)
        {
            Message = message;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, Message: {Message})";
        }
    }
}
