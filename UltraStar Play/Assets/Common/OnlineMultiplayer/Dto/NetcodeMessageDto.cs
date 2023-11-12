namespace CommonOnlineMultiplayer
{
    public class NetcodeMessageDto : JsonSerializable
    {
        public ENetcodeMessageType MessageType { get; private set; }

        public NetcodeMessageDto()
        {
        }

        public NetcodeMessageDto(ENetcodeMessageType messageType)
        {
            MessageType = messageType;
        }
    }
}
