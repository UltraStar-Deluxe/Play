namespace SteamOnlineMultiplayer
{
    public class SteamLobbyMemberAddedEvent : SteamLobbyMemberChangedEvent
    {
        public SteamLobbyMemberAddedEvent(SteamLobbyMember steamLobbyMember)
            : base(steamLobbyMember)
        {
        }
    }
}
