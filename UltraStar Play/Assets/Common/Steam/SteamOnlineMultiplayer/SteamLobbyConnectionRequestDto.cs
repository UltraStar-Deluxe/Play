using CommonOnlineMultiplayer;

namespace SteamOnlineMultiplayer
{
    public class SteamLobbyConnectionRequestDto : LobbyConnectionRequestDto
    {
        public ulong SteamId { get; private set; }

        public SteamLobbyConnectionRequestDto()
        {
        }

        public SteamLobbyConnectionRequestDto(string displayName, ulong steamId) : base(displayName)
        {
            SteamId = steamId;
        }
    }
}
