namespace CommonOnlineMultiplayer
{
    public abstract class LobbyMemberConnectionChangedEvent
    {
        public UnityNetcodeClientId UnityNetcodeClientId { get; private set; }

        protected LobbyMemberConnectionChangedEvent(UnityNetcodeClientId unityNetcodeClientId)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
        }
    }
}
