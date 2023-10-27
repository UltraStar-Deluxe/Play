using Unity.Collections;

namespace SteamOnlineMultiplayer
{
    public struct MemberData
    {
        public SteamIdSerializable SteamIdData { get; private set; }
        public FixedString32Bytes DisplayName { get; private set; }
        public ulong ClientId { get; private set; }

        public MemberData(SteamIdSerializable steamId, FixedString32Bytes playerName, ulong clientId)
        {
            SteamIdData = steamId;
            DisplayName = playerName;
            ClientId = clientId;
        }
    }
}
