using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public struct NamedMessage
    {
        public ulong SenderNetcodeClientId { get; private set; }
        public FastBufferReader MessagePayload { get; private set; }

        public NamedMessage(
            ulong senderNetcodeClientId,
            FastBufferReader messagePayload)
        {
            SenderNetcodeClientId = senderNetcodeClientId;
            MessagePayload = messagePayload;
        }
    }
}
