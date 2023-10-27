namespace SteamOnlineMultiplayer
{
    public enum ConnectStatus : byte
    {
        Undefined,
        Success,
        ServerFull,
        GameInProgress,
        LoggedInAgain,
        UserRequestedDisconnect,
        GenericDisconnect,
        KickDisconnect,
        BanDisconnect,
    }
}
