using CommonOnlineMultiplayer;

namespace SteamOnlineMultiplayer
{
    public abstract class NetworkClientConnectionChangedEvent
    {
        public UnityNetcodeClientId UnityNetcodeClientId { get; private set; }

        protected NetworkClientConnectionChangedEvent(UnityNetcodeClientId unityNetcodeClientId)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
        }
    }
}
