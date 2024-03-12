namespace CommonOnlineMultiplayer
{
    public abstract class NetcodeResponseDto : NetcodeMessageDto
    {
        protected NetcodeResponseDto() : base(ENetcodeMessageType.Other)
        {
        }

        protected NetcodeResponseDto(ENetcodeMessageType messageType) : base(messageType)
        {
        }
    }
}
