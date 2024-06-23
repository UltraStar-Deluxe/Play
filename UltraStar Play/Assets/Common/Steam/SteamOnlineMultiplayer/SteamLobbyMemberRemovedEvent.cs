namespace SteamOnlineMultiplayer
{
    public class SteamLobbyMemberRemovedEvent : SteamLobbyMemberChangedEvent
    {
        public SteamLobbyMemberRemovedEvent(SteamLobbyMember steamLobbyMember)
            : base(steamLobbyMember)
        {
        }
    }
}
