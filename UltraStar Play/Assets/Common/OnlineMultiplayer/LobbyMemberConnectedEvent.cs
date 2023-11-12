namespace CommonOnlineMultiplayer
{
    public class LobbyMemberConnectedEvent : AbstractLobbyMemberConnectionChangedEvent
    {
        public LobbyMemberConnectedEvent(UnityNetcodeClientId unityNetcodeClientId)
            : base(unityNetcodeClientId)
        {
        }
    }
}
