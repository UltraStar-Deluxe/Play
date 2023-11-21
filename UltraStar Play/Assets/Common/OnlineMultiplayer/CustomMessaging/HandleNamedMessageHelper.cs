using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public struct HandleNamedMessageHelper
    {
        private readonly string messageName;
        private readonly IReadOnlyList<NamedMessageHandler> messageHandlers;

        public HandleNamedMessageHelper(
            string messageName,
            IReadOnlyList<NamedMessageHandler> messageHandlers)
        {
            this.messageName = messageName;
            this.messageHandlers = messageHandlers;
        }

        public void HandleNamedMessage(ulong senderNetcodeClientId, FastBufferReader messagePayload)
        {
            int messageLength = messagePayload.Length - messagePayload.Position;
            if (messageLength <= 0)
            {
                return;
            }

            string messageNameLocal = this.messageName;
            Log.Verbose(() => $"Received message {messageNameLocal} with length {messageLength} from Netcode client {senderNetcodeClientId}");

            if (messageHandlers.Count == 1)
            {
                messageHandlers[0].handleMessage?.Invoke(senderNetcodeClientId, messagePayload);
            }
            else
            {
                // The reader can only be read once.
                // Thus, for multiple handlers, we need to make a copy of the data.
                byte[] messageBytes = new byte[messageLength];
                if (!messagePayload.TryBeginRead(messageBytes.Length))
                {
                    Debug.LogError($"Failed to read message bytes from FastBufferReader. Attempt to read {messageBytes.Length} bytes, length is {messagePayload.Length}, position is {messagePayload.Position}");
                }
                messagePayload.ReadBytes(ref messageBytes, messageBytes.Length, 0);

                foreach (NamedMessageHandler messageHandler in messageHandlers)
                {
                    using FastBufferReader readerCopy = new(messageBytes, Allocator.Temp);
                    messageHandler.handleMessage?.Invoke(senderNetcodeClientId, readerCopy);
                }
            }
        }
    }
}
