using System.Collections.Generic;
using SteamOnlineMultiplayer;

namespace CommonOnlineMultiplayer
{
    public class ConnectedMemberDatasResponseDto : NetcodeCustomMessageDto
    {
        public List<MemberData> MemberDatas { get; set; }

        public ConnectedMemberDatasResponseDto()
            : base(EOnlineMultiplayerMessageType.MemberDatasResponse)
        {
        }
    }
}
