namespace CommonOnlineMultiplayer
{
    public class NetcodeRequestDto : NetcodeMessageDto
    {
        public NetcodeRequestDto()
            : base(ENetcodeMessageType.Other)
        {
        }

        public NetcodeRequestDto(ENetcodeMessageType messageType) : base(messageType)
        {
        }
    }
}
