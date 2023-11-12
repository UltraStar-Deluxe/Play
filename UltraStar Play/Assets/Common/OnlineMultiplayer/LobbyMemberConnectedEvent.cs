namespace CommonOnlineMultiplayer
{
    public class LobbyMemberConnectedEvent : LobbyMemberConnectionChangedEvent
    {
        public LobbyMemberConnectedEvent(UnityNetcodeClientId unityNetcodeClientId)
            : base(unityNetcodeClientId)
        {
        }
    }
}
