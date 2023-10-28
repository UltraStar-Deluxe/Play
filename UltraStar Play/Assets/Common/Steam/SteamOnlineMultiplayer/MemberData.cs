using System;
using CommonOnlineMultiplayer;
using Steamworks;

namespace SteamOnlineMultiplayer
{
    [Serializable]
    public struct MemberData
    {
        public UnityNetcodeClientId UnityNetcodeClientId { get; private set; }
        public SteamId SteamId { get; private set; }
        public string ConnectionGuid { get; private set; }
        public string DisplayName { get; private set; }
        public string CurrentSceneName { get; set; }

        public MemberData(
            UnityNetcodeClientId unityNetcodeClientId,
            SteamId steamId,
            string connectionGuid,
            string displayName,
            string currentSceneName)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
            SteamId = steamId;
            ConnectionGuid = connectionGuid;
            DisplayName = displayName;
            CurrentSceneName = currentSceneName;
        }
    }
}
