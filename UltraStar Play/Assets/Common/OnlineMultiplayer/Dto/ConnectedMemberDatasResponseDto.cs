using System.Collections.Generic;

namespace CommonOnlineMultiplayer
{
    public class CurrentLobbyMembersResponseDto : NetcodeResponseDto
    {
        public List<LobbyMember> LobbyMembers { get; set; }

        public CurrentLobbyMembersResponseDto()
            : base(ENetcodeMessageType.CurrentLobbyMembersResponse)
        {
        }
    }
}
