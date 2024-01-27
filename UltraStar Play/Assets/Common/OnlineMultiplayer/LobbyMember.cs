using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    /**
     * Represents a connected client, i.e., a client that has joined a hosted online game.
     * A hosted game is called Lobby and connected clients are called LobbyMembers.
     * A Lobby corresponds to a Netcode server (like in NetworkManager.Singleton.IsServer)
     * whereas the LobbyMember corresponds to a connected Netcode client (like in NetworkManager.Singleton.IsClient)
     */
    [Serializable]
    public class LobbyMember
    {
        public string DisplayName { get; private set; }
        public UnityNetcodeClientId UnityNetcodeClientId { get; private set; }
        public bool IsHost => UnityNetcodeClientId == NetworkManager.ServerClientId;

        public LobbyMember()
        {
        }

        public LobbyMember(
            UnityNetcodeClientId unityNetcodeClientId,
            string displayName)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return $"{nameof(LobbyMember)}(DisplayName: '{DisplayName}', UnityNetcodeClientId: {UnityNetcodeClientId})";
        }
    }
}
