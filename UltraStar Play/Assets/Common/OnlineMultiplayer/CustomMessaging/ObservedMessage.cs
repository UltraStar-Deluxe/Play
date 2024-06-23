using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public struct ObservedMessage
    {
        public ulong SenderNetcodeClientId { get; private set; }
        public string ObservedMessageName { get; private set; }
        public FastBufferReader MessagePayload { get; private set; }

        public ObservedMessage(
            ulong senderNetcodeClientId,
            string observedMessageName,
            FastBufferReader messagePayload)
        {
            SenderNetcodeClientId = senderNetcodeClientId;
            ObservedMessageName = observedMessageName;
            MessagePayload = messagePayload;
        }
    }
}
