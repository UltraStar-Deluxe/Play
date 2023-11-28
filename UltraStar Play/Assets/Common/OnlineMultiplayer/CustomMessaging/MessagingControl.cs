using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class MessagingControl
    {
        private const string ForwardToClientsMessageName = "FORWARD_TO_CLIENTS";
        private const string ForwardToClientMessageName = "FORWARD_TO_CLIENT";

        private readonly Dictionary<string, List<NamedMessageHandler>> messageNameToHandlers = new();
        private readonly Dictionary<string, HandleNamedMessageHelper> messageNameToHandleNamedMessageHelper = new();

        private bool hasRegisteredForwardNamedMessageHandlers;

        private NetworkManager NetworkManager => NetworkManager.Singleton;

        public void RegisterNamedMessageHandlersToForwardMessagesIfNeeded()
        {
            if (hasRegisteredForwardNamedMessageHandlers
                || !NetworkManager.IsServer)
            {
                return;
            }
            hasRegisteredForwardNamedMessageHandlers = true;

            RegisterNamedMessageHandler(
                ForwardToClientsMessageName,
                ForwardNamedMessageToClients);

            RegisterNamedMessageHandler(
                ForwardToClientMessageName,
                ForwardNamedMessageToClient);
        }

        private void ForwardNamedMessageToClient(NamedMessage request)
        {
            Debug.Log($"Forwarding named message from client {request.SenderNetcodeClientId} to single client");

            using FastBufferWriter originalMessageWriter = ReadForwardedFastBufferReader(
                request.MessagePayload,
                out string messageName,
                out ulong[] targetNetcodeClientIds,
                out NetworkDelivery networkDelivery);

            SendNamedMessageToClient(
                messageName,
                originalMessageWriter,
                targetNetcodeClientIds.First(),
                networkDelivery);
        }

        private void ForwardNamedMessageToClients(NamedMessage request)
        {
            Debug.Log($"Forwarding named message from client {request.SenderNetcodeClientId} to multiple clients");

            using FastBufferWriter originalMessageWriter = ReadForwardedFastBufferReader(
                request.MessagePayload,
                out string messageName,
                out ulong[] targetNetcodeClientIds,
                out NetworkDelivery networkDelivery);

            SendNamedMessageToClients(
                messageName,
                originalMessageWriter,
                targetNetcodeClientIds,
                networkDelivery);
        }

        public void SendNamedMessageToClients(
            string messageName,
            FastBufferWriter fastBufferWriter,
            IReadOnlyList<ulong> targetNetcodeClientIds,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            if (targetNetcodeClientIds.IsNullOrEmpty())
            {
                return;
            }

            if (targetNetcodeClientIds.Count == 1)
            {
                SendNamedMessageToClient(
                    messageName,
                    fastBufferWriter,
                    targetNetcodeClientIds[0],
                    networkDelivery);
                return;
            }

            Log.Verbose(() => $"Sending message {messageName} to Netcode clients {targetNetcodeClientIds.ToCsv()}");

            if (NetworkManager.IsServer)
            {
                NetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    targetNetcodeClientIds,
                    fastBufferWriter,
                    networkDelivery);
            }
            else if (NetworkManager.IsClient)
            {
                // Only the server can send to clients directly. Other clients can only send to the server.
                // We are not the server, thus we need to sent the message to the server, which then forwards it to the clients.
                using FastBufferWriter forwardedFastBufferWriter = CreateForwardedFastBufferWriter(
                    messageName,
                    targetNetcodeClientIds.ToArray(),
                    networkDelivery,
                    fastBufferWriter);
                SendNamedMessageToServer(
                    ForwardToClientsMessageName,
                    forwardedFastBufferWriter,
                    networkDelivery);
            }

            fastBufferWriter.Dispose();
        }

        public void SendNamedMessageToClient(
            string messageName,
            FastBufferWriter fastBufferWriter,
            ulong targetNetcodeClientId,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            Log.Verbose(() => $"Sending message {messageName} to Netcode client {targetNetcodeClientId}");

            if (NetworkManager.IsServer
                || targetNetcodeClientId == NetworkManager.ServerClientId)
            {
                NetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    targetNetcodeClientId,
                    fastBufferWriter,
                    networkDelivery);
            }
            else if (NetworkManager.IsClient)
            {
                // Only the server can send to clients directly. Other clients can only send to the server.
                // We are not the server and do not send to the server,
                // thus we need to sent the message to the server, which then forwards it to the clients.
                using FastBufferWriter forwardedFastBufferWriter = CreateForwardedFastBufferWriter(
                    messageName,
                    new ulong[] { targetNetcodeClientId },
                    networkDelivery,
                    fastBufferWriter);
                SendNamedMessageToServer(
                    "FORWARD_TO_CLIENT",
                    forwardedFastBufferWriter,
                    networkDelivery);
            }

            fastBufferWriter.Dispose();
        }

        private FastBufferWriter CreateForwardedFastBufferWriter(
            string messageName,
            ulong[] targetNetcodeClientIds,
            NetworkDelivery networkDelivery,
            FastBufferWriter originalFastBufferWriter)
        {
            int size = FastBufferWriter.GetWriteSize(messageName)
                       + FastBufferWriter.GetWriteSize(targetNetcodeClientIds)
                       + FastBufferWriter.GetWriteSize<NetworkDelivery>()
                       + originalFastBufferWriter.Length;
            FastBufferWriter forwardedFastBufferWriter = new(size, Allocator.Temp);
            forwardedFastBufferWriter.WriteValueSafe(messageName);
            forwardedFastBufferWriter.WriteValueSafe(targetNetcodeClientIds);
            forwardedFastBufferWriter.WriteValueSafe(networkDelivery);
            forwardedFastBufferWriter.TryBeginWrite(originalFastBufferWriter.Length);
            forwardedFastBufferWriter.CopyFrom(originalFastBufferWriter);

            return forwardedFastBufferWriter;
        }

        private FastBufferWriter ReadForwardedFastBufferReader(
            FastBufferReader forwardedFastBufferReader,
            out string messageName,
            out ulong[] targetNetcodeClientIds,
            out NetworkDelivery networkDelivery)
        {
            forwardedFastBufferReader.ReadValueSafe(out messageName);
            forwardedFastBufferReader.ReadValueSafe(out targetNetcodeClientIds);
            forwardedFastBufferReader.ReadValueSafe(out networkDelivery);

            int originalMessageLength = forwardedFastBufferReader.Length - forwardedFastBufferReader.Position;
            byte[] originalMessageBytes = new byte[originalMessageLength];
            forwardedFastBufferReader.ReadBytes(ref originalMessageBytes, originalMessageBytes.Length, 0);

            FastBufferWriter originalFastBufferWriter = new();
            originalFastBufferWriter.WriteBytes(originalMessageBytes);

            return originalFastBufferWriter;
        }

        private void SendNamedMessageToServer(
            string messageName,
            FastBufferWriter fastBufferWriter,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            NetworkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                NetworkManager.ServerClientId,
                fastBufferWriter,
                networkDelivery);
        }

        public IDisposable RegisterNamedMessageHandler(
            string messageName,
            Action<NamedMessage> handleMessage)
        {
            if (!messageNameToHandlers.TryGetValue(messageName, out List<NamedMessageHandler> messageHandlers))
            {
                messageHandlers = new();
                messageNameToHandlers[messageName] = messageHandlers;
            }

            if (!messageNameToHandleNamedMessageHelper.ContainsKey(messageName))
            {
                HandleNamedMessageHelper handleNamedMessageHelper = new HandleNamedMessageHelper(messageName, messageHandlers);
                messageNameToHandleNamedMessageHelper[messageName] = handleNamedMessageHelper;
                NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, handleNamedMessageHelper.HandleNamedMessage);
            }

            NamedMessageHandler namedMessageHandler = new NamedMessageHandler(handleMessage);

            messageHandlers.Add(namedMessageHandler);

            Debug.Log($"RegisterNamedMessageHandler - new count of handlers for message name '{messageName}': {messageHandlers.Count}");

            bool isDisposed = false;
            return Disposable.Create(() =>
            {
                if (isDisposed)
                {
                    return;
                }
                isDisposed = true;

                messageHandlers.Remove(namedMessageHandler);
                if (messageHandlers.IsNullOrEmpty())
                {
                    messageNameToHandlers.Remove(messageName);
                }
                Debug.Log($"RemoveNamedMessageHandler - new count of handlers for message name '{messageName}': {messageHandlers.Count}");
            });
        }
    }
}
