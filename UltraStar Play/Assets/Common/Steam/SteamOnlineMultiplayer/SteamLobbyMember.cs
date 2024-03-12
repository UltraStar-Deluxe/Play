using System;
using CommonOnlineMultiplayer;
using Steamworks;

namespace SteamOnlineMultiplayer
{
    [Serializable]
    public class SteamLobbyMember : LobbyMember
    {
        public SteamId SteamId { get; private set; }

        public SteamLobbyMember()
        {
        }

        public SteamLobbyMember(
            UnityNetcodeClientId unityNetcodeClientId,
            string displayName,
            SteamId steamId) : base(unityNetcodeClientId, displayName)
        {
            SteamId = steamId;
        }

        public override string ToString()
        {
            return $"{nameof(SteamLobbyMember)}(DisplayName: '{DisplayName}', UnityNetcodeClientId: {UnityNetcodeClientId}, SteamId: {SteamId})";
        }
    }
}
