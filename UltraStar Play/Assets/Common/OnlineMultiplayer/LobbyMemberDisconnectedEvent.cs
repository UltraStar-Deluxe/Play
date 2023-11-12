namespace CommonOnlineMultiplayer
{
    public class LobbyMemberDisconnectedEvent : AbstractLobbyMemberConnectionChangedEvent
    {
        public LobbyMemberDisconnectedEvent(UnityNetcodeClientId unityNetcodeClientId)
            : base(unityNetcodeClientId)
        {
        }
    }
}
