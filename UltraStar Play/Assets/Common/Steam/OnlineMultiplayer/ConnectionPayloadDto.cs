using Unity.Collections;

namespace SteamOnlineMultiplayer
{
    public class ConnectionPayloadDto
    {
        public int clientScene = -1;
        public FixedString32Bytes clientGUID;
        public FixedString32Bytes displayName;
    }
}
