namespace CommonOnlineMultiplayer
{
    public class NetcodeCustomMessageDto : JsonSerializable
    {
        public EOnlineMultiplayerMessageType MessageType { get; private set; }

        public NetcodeCustomMessageDto()
        {
        }

        public NetcodeCustomMessageDto(EOnlineMultiplayerMessageType messageType)
        {
            MessageType = messageType;
        }
    }
}
