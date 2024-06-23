namespace CommonOnlineMultiplayer
{
    /**
     * Hosts and joins online games.
     * Holds a reference to the currently joined online game.
     */
    public interface ILobbyManager
    {
        ILobby CurrentLobby { get; }
        void LeaveCurrentLobby();
    }
}
