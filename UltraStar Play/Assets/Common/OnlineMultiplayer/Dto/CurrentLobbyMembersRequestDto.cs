namespace CommonOnlineMultiplayer
{
    public class CurrentLobbyMembersRequestDto : NetcodeRequestDto
    {
        public CurrentLobbyMembersRequestDto()
            : base(ENetcodeMessageType.CurrentLobbyMembersRequest)
        {
        }
    }
}
