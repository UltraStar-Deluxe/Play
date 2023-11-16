using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    [Serializable]
    public class LobbyMemberNetworkSerializable : INetworkSerializable
    {
        public string displayName;
        public ulong unityNetcodeClientId;

        public LobbyMemberNetworkSerializable()
        {
        }

        public LobbyMemberNetworkSerializable(LobbyMember lobbyMember)
        {
            displayName = lobbyMember.DisplayName;
            unityNetcodeClientId = lobbyMember.UnityNetcodeClientId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref displayName);
            serializer.SerializeValue(ref unityNetcodeClientId);
        }
    }
}
