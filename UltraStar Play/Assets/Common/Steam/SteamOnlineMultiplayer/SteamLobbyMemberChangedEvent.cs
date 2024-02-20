namespace SteamOnlineMultiplayer
{
    public abstract class SteamLobbyMemberChangedEvent
    {
        public SteamLobbyMember SteamLobbyMember { get; private set; }

        protected SteamLobbyMemberChangedEvent(SteamLobbyMember steamLobbyMember)
        {
            SteamLobbyMember = steamLobbyMember;
        }
    }
}
