namespace CommonOnlineMultiplayer
{
    public class LobbyMemberDisconnectedEvent : LobbyMemberConnectionChangedEvent
    {
        public LobbyMemberDisconnectedEvent(UnityNetcodeClientId unityNetcodeClientId)
            : base(unityNetcodeClientId)
        {
        }
    }
}
