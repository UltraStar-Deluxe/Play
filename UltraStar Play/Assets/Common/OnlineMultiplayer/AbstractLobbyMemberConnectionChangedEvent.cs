namespace CommonOnlineMultiplayer
{
    public abstract class AbstractLobbyMemberConnectionChangedEvent
    {
        public UnityNetcodeClientId UnityNetcodeClientId { get; private set; }

        protected AbstractLobbyMemberConnectionChangedEvent(UnityNetcodeClientId unityNetcodeClientId)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
        }
    }
}
