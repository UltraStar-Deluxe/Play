using System.Collections.Generic;
using CommonOnlineMultiplayer;
using Steamworks;

namespace SteamOnlineMultiplayer
{
    public class ConnectedMemberDataRegistry
    {
        private readonly List<MemberData> datas = new();
        private readonly Dictionary<ulong, MemberData> unityNetcodeClientIdToData = new();
        private readonly Dictionary<SteamId, MemberData> steamIdToData = new();
        public int Count => datas.Count;

        public void Clear()
        {
            datas.Clear();
            unityNetcodeClientIdToData.Clear();
            steamIdToData.Clear();
        }

        public IReadOnlyList<MemberData> GetAllData()
        {
            return datas;
        }

        public void Add(MemberData data)
        {
            datas.Add(data);
            unityNetcodeClientIdToData[data.UnityNetcodeClientId] = data;
            steamIdToData[data.SteamId] = data;
        }

        public void Remove(MemberData data)
        {
            datas.Remove(data);
        }

        public bool TryGetDataBySteamId(SteamId steamId, out MemberData data)
        {
            return steamIdToData.TryGetValue(steamId, out data);
        }

        public bool TryGetDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId, out MemberData data)
        {
            return unityNetcodeClientIdToData.TryGetValue(netcodeClientId, out data);
        }
    }
}
