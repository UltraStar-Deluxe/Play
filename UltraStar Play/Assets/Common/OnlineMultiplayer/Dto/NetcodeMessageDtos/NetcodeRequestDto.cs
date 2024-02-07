namespace CommonOnlineMultiplayer
{
    public abstract class NetcodeRequestDto : NetcodeMessageDto
    {
        protected NetcodeRequestDto()
            : base(ENetcodeMessageType.Other)
        {
        }

        protected NetcodeRequestDto(ENetcodeMessageType messageType) : base(messageType)
        {
        }
    }
}
