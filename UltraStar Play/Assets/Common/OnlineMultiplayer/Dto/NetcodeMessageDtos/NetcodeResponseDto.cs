namespace CommonOnlineMultiplayer
{
    public class NetcodeResponseDto : NetcodeMessageDto
    {
        public NetcodeResponseDto()
        {
        }

        public NetcodeResponseDto(ENetcodeMessageType messageType) : base(messageType)
        {
        }
    }
}
