namespace CommonOnlineMultiplayer
{
    public abstract class NetcodeMessageDto : JsonSerializable
    {
        public string MessageType { get; private set; }

        protected NetcodeMessageDto()
        {
            MessageType = GetType().Name;
        }
    }
}
