namespace CommonOnlineMultiplayer
{
    public class ConnectedMemberDatasRequestDto : NetcodeCustomMessageDto
    {
        public ConnectedMemberDatasRequestDto()
            : base(EOnlineMultiplayerMessageType.MemberDatasRequest)
        {
        }
    }
}
