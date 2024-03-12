namespace SteamOnlineMultiplayer
{
    public struct SteamLobbyConfig
    {
        public string name;
        public bool joinable;
        public ESteamLobbyVisibility visibility;
        public byte maxMembers;
        public string password;
    }
}
