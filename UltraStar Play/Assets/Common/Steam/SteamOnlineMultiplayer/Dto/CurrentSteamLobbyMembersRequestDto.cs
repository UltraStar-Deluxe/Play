using System.Collections.Generic;
using CommonOnlineMultiplayer;

namespace SteamOnlineMultiplayer
{
    public class CurrentSteamLobbyMembersResponseDto : NetcodeMessageDto
    {
        public List<SteamLobbyMember> SteamLobbyMembers { get; set; }

        public CurrentSteamLobbyMembersResponseDto()
            : base(ENetcodeMessageType.CurrentLobbyMembersResponse)
        {
        }
    }
}
