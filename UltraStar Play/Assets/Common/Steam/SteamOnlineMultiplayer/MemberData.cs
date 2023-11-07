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

        public MemberData(
            UnityNetcodeClientId unityNetcodeClientId,
            SteamId steamId,
            string displayName)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
            SteamId = steamId;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return $"{nameof(MemberData)}(DisplayName: {DisplayName}, SteamId: {SteamId}, UnityNetcodeClientId: {UnityNetcodeClientId})";
        }
    }
}
