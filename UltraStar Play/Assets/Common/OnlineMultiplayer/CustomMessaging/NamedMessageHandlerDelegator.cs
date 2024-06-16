using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class NamedMessageHandlerDelegator
    {
        private readonly string messageName;
        private readonly List<NamedMessageHandler> messageHandlers = new();

        public NamedMessageHandlerDelegator(string messageName)
        {
            this.messageName = messageName;
        }

        public int Count => messageHandlers.Count;

        public void Add(NamedMessageHandler handler)
        {
            messageHandlers.AddIfNotContains(handler);
        }

        public void Remove(NamedMessageHandler handler)
        {
            messageHandlers.Remove(handler);
        }

        public void HandleNamedMessage(ulong senderNetcodeClientId, FastBufferReader messagePayload)
        {
            int messageLength = messagePayload.Length - messagePayload.Position;
            if (messageLength <= 0)
            {
                return;
            }

            string messageNameLocal = this.messageName;
            IReadOnlyList<NamedMessageHandler> messageHandlersLocalRef = messageHandlers;
            Log.Verbose(() => $"Received message {messageNameLocal} with length {messageLength} from Netcode client {senderNetcodeClientId}. Registered message handlers: {messageHandlersLocalRef.Count}");

            if (messageHandlers.Count == 1)
            {
                messageHandlers[0].handleMessage?.Invoke(new NamedMessage(senderNetcodeClientId, messagePayload));
            }
            else if (messageHandlers.Count > 1)
            {
                // The FastBufferReader can only be read once.
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
                    messageHandler.handleMessage?.Invoke(new NamedMessage(senderNetcodeClientId, readerCopy));
                }
            }
            else if (messageHandlers.Count <= 0)
            {
                Debug.LogWarning($"No handler found for message {messageNameLocal} from Netcode client {senderNetcodeClientId}");
            }
        }
    }
}
