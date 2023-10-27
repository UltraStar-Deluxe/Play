using Steamworks;
using Unity.Netcode;

namespace SteamOnlineMultiplayer
{
    public struct SteamIdSerializable : INetworkSerializable
    {
        public SteamId steamId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter =>
            serializer.SerializeValue(ref steamId.Value);

        public static implicit operator SteamIdSerializable(SteamId value)
        {
            SteamIdSerializable result = default(SteamIdSerializable);
            result.steamId = value;
            return result;
        }
    }
}
