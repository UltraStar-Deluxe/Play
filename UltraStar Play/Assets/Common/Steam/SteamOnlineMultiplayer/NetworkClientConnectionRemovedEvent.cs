using CommonOnlineMultiplayer;

namespace SteamOnlineMultiplayer
{
    public class NetworkClientConnectionRemovedEvent : NetworkClientConnectionChangedEvent
    {
        public NetworkClientConnectionRemovedEvent(UnityNetcodeClientId unityNetcodeClientId)
            : base(unityNetcodeClientId)
        {
        }
    }
}
