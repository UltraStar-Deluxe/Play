using System;
using System.Collections.Generic;

namespace CommonOnlineMultiplayer
{
    [Serializable]
    public class LobbyMemberRegistry
    {
        private readonly List<LobbyMember> lobbyMembers = new();
        private readonly Dictionary<ulong, LobbyMember> unityNetcodeClientIdToLobbyMembers = new();
        public int Count => lobbyMembers.Count;

        public void Clear()
        {
            lobbyMembers.Clear();
            unityNetcodeClientIdToLobbyMembers.Clear();
        }

        public IReadOnlyList<LobbyMember> GetAllLobbyMembers()
        {
            return lobbyMembers;
        }

        public void Add(LobbyMember lobbyMember)
        {
            lobbyMembers.Add(lobbyMember);
            unityNetcodeClientIdToLobbyMembers[lobbyMember.UnityNetcodeClientId] = lobbyMember;
        }

        public void Remove(LobbyMember lobbyMember)
        {
            lobbyMembers.Remove(lobbyMember);
        }

        public bool TryGetDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId, out LobbyMember lobbyMember)
        {
            return unityNetcodeClientIdToLobbyMembers.TryGetValue(netcodeClientId, out lobbyMember);
        }
    }
}
