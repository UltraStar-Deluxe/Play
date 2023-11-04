using System;
using CommonOnlineMultiplayer;
using Steamworks;

namespace SteamOnlineMultiplayer
{
    [Serializable]
    public struct MemberData
    {
        public string DisplayName { get; private set; }
        public SteamId SteamId { get; private set; }
        public UnityNetcodeClientId UnityNetcodeClientId { get; private set; }
        public string CurrentSceneName { get; set; }
        public string ConnectionGuid { get; private set; }

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

        public override string ToString()
        {
            return $"{nameof(MemberData)}(DisplayName: {DisplayName}, SteamId: {SteamId}, UnityNetcodeClientId: {UnityNetcodeClientId})";
        }
    }
}
