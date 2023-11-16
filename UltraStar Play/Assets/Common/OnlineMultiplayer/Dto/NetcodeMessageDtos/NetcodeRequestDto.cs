namespace CommonOnlineMultiplayer
{
    public class NetcodeRequestDto : NetcodeMessageDto
    {
        public NetcodeRequestDto()
        {
        }

        public NetcodeRequestDto(ENetcodeMessageType messageType) : base(messageType)
        {
        }
    }
}
