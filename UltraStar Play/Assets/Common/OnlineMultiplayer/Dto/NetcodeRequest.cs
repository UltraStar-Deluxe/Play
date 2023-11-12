namespace CommonOnlineMultiplayer
{
    public class NetcodeRequest
    {
        public string RequestMessage { get; private set; }
        public UnityNetcodeClientId SenderNetcodeClientId { get; private set; }

        public NetcodeRequest(string requestMessage, UnityNetcodeClientId senderNetcodeClientId)
        {
            RequestMessage = requestMessage;
            SenderNetcodeClientId = senderNetcodeClientId;
        }
    }
}
