using CommonOnlineMultiplayer;

namespace SteamOnlineMultiplayer
{
    public class NetworkClientConnectionAddedEvent : NetworkClientConnectionChangedEvent
    {
        public NetworkClientConnectionAddedEvent(UnityNetcodeClientId unityNetcodeClientId)
            : base(unityNetcodeClientId)
        {
        }
    }
}
