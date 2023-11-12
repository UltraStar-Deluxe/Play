using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using Steamworks;

namespace SteamOnlineMultiplayer
{
    [Serializable]
    public class SteamLobbyMemberRegistry
    {
        private readonly List<SteamLobbyMember> datas = new();
        private readonly Dictionary<ulong, SteamLobbyMember> unityNetcodeClientIdToData = new();
        private readonly Dictionary<SteamId, SteamLobbyMember> steamIdToData = new();
        public int Count => datas.Count;

        public void Clear()
        {
            datas.Clear();
            unityNetcodeClientIdToData.Clear();
            steamIdToData.Clear();
        }

        public IReadOnlyList<SteamLobbyMember> GetAllData()
        {
            return datas;
        }

        public void Add(SteamLobbyMember data)
        {
            datas.Add(data);
            unityNetcodeClientIdToData[data.UnityNetcodeClientId] = data;
            steamIdToData[data.SteamId] = data;
        }

        public void Remove(SteamLobbyMember data)
        {
            datas.Remove(data);
        }

        public bool TryGetDataBySteamId(SteamId steamId, out SteamLobbyMember data)
        {
            return steamIdToData.TryGetValue(steamId, out data);
        }

        public bool TryGetDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId, out SteamLobbyMember data)
        {
            return unityNetcodeClientIdToData.TryGetValue(netcodeClientId, out data);
        }
    }
}
