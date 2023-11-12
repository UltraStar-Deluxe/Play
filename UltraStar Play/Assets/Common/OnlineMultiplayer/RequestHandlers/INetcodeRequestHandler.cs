namespace CommonOnlineMultiplayer
{
    public interface INetcodeRequestHandler
    {
        public ENetcodeMessageType HandledMessageType { get; }
        public string GetResponse(NetcodeRequest request);
        public int Priority { get; }
    }
}
