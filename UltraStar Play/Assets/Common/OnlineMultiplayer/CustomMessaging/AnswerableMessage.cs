using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public struct AnswerableMessage
    {
        public ulong SenderNetcodeClientId { get; private set; }
        public string ResponseMessageName { get; private set; }
        public FastBufferReader MessagePayload { get; private set; }

        public AnswerableMessage(
            ulong senderNetcodeClientId,
            string responseMessageName,
            FastBufferReader messagePayload)
        {
            SenderNetcodeClientId = senderNetcodeClientId;
            ResponseMessageName = responseMessageName;
            MessagePayload = messagePayload;
        }
    }
}
