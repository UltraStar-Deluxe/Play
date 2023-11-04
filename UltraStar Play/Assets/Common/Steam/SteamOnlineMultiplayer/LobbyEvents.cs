using Steamworks;
using Steamworks.Data;

namespace SteamOnlineMultiplayer
{
    public class LobbyEvent
    {
        public Lobby Lobby { get; private set; }

        public LobbyEvent(Lobby lobby)
        {
            Lobby = lobby;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id})";
        }
    }

    public class LobbyCreatedEvent : LobbyEvent
    {
        public LobbyCreatedEvent(Lobby lobby) : base(lobby)
        {
        }
    }

    public class LobbyDataChangedEvent : LobbyEvent
    {
        public LobbyDataChangedEvent(Lobby lobby) : base(lobby)
        {
        }
    }

    public class LobbyEnteredEvent : LobbyEvent
    {
        public LobbyEnteredEvent(Lobby lobby) : base(lobby)
        {
        }
    }

    public class HasFriendLobbyEvent : LobbyEvent
    {
        public Friend Friend { get; set; }

        public HasFriendLobbyEvent(Lobby lobby, Friend friend) : base(lobby)
        {
            Friend = friend;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, Friend: {Friend})";
        }
    }

    public class MemberJoinedLobbyEvent : HasFriendLobbyEvent
    {
        public MemberJoinedLobbyEvent(Lobby lobby, Friend friend) : base(lobby, friend)
        {
        }
    }

    public class MemberLeftLobbyEvent : HasFriendLobbyEvent
    {
        public MemberLeftLobbyEvent(Lobby lobby, Friend friend) : base(lobby, friend)
        {
        }
    }

    public class MemberInviteReceivedLobbyEvent : HasFriendLobbyEvent
    {
        public MemberInviteReceivedLobbyEvent(Lobby lobby, Friend friend) : base(lobby, friend)
        {
        }
    }

    public class MemberKickedLobbyEvent : LobbyEvent
    {
        public Friend ExecutingMember { get; private set; }
        public Friend KickedMember { get; private set; }

        public MemberKickedLobbyEvent(Lobby lobby, Friend executingMember, Friend kickedMember) : base(lobby)
        {
            ExecutingMember = executingMember;
            KickedMember = kickedMember;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, ExecutingMember: {ExecutingMember}, KickedMember: {KickedMember})";
        }
    }

    public class MemberBannedLobbyEvent : LobbyEvent
    {
        public Friend ExecutingMember { get; private set; }
        public Friend BannedMember { get; private set; }

        public MemberBannedLobbyEvent(Lobby lobby, Friend executingMember, Friend bannedMember) : base(lobby)
        {
            ExecutingMember = executingMember;
            BannedMember = bannedMember;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, ExecutingMember: {ExecutingMember}, BannedMember: {BannedMember})";
        }
    }

    public class LobbyChatMessageReceivedEvent : HasFriendLobbyEvent
    {
        public string Message { get; private set; }

        public LobbyChatMessageReceivedEvent(Lobby lobby, Friend friend, string message) : base(lobby, friend)
        {
            Message = message;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Lobby: {Lobby.Id}, Message: {Message})";
        }
    }
}
