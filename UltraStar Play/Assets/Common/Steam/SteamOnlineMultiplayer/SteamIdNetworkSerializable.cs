using Steamworks;
using Unity.Netcode;

namespace SteamOnlineMultiplayer
{
    public struct SteamIdNetworkSerializable : INetworkSerializable
    {
        private SteamId steamId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref steamId.Value);
        }

        public static implicit operator SteamIdNetworkSerializable(SteamId value)
        {
            SteamIdNetworkSerializable result = default;
            result.steamId = value;
            return result;
        }
    }
}
