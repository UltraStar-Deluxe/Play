using System;
using System.Collections.Generic;

namespace CommonOnlineMultiplayer
{
    [Serializable]
    public class LobbyMemberRegistry
    {
        private readonly List<LobbyMember> datas = new();
        private readonly Dictionary<ulong, LobbyMember> unityNetcodeClientIdToData = new();
        public int Count => datas.Count;

        public void Clear()
        {
            datas.Clear();
            unityNetcodeClientIdToData.Clear();
        }

        public IReadOnlyList<LobbyMember> GetAllData()
        {
            return datas;
        }

        public void Add(LobbyMember lobbyMember)
        {
            datas.Add(lobbyMember);
            unityNetcodeClientIdToData[lobbyMember.UnityNetcodeClientId] = lobbyMember;
        }

        public void Remove(LobbyMember lobbyMember)
        {
            datas.Remove(lobbyMember);
        }

        public bool TryGetDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId, out LobbyMember lobbyMember)
        {
            return unityNetcodeClientIdToData.TryGetValue(netcodeClientId, out lobbyMember);
        }
    }
}
