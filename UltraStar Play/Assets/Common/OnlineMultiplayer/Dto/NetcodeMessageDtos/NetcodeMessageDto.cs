namespace CommonOnlineMultiplayer
{
    public abstract class NetcodeMessageDto : JsonSerializable
    {
        public ENetcodeMessageType MessageType { get; private set; }

        protected NetcodeMessageDto(ENetcodeMessageType messageType)
        {
            MessageType = messageType;
        }
    }
}
