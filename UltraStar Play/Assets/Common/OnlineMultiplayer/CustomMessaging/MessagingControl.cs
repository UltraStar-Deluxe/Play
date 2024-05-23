using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class MessagingControl : IMessagingControl
    {
        private const string ForwardToClientsMessageName = "FORWARD_TO_CLIENTS";
        private const string ForwardToClientMessageName = "FORWARD_TO_CLIENT";

        private readonly NetworkManager networkManager;

        private readonly Dictionary<string, NamedMessageHandlerDelegator> messageNameToHandleNamedMessageHelper = new();

        public MessagingControl(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
        }

        public void RegisterNamedMessageHandlersToForwardMessages()
        {
            if (!networkManager.IsServer)
            {
                return;
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ForwardToClientsMessageName);
            RegisterNamedMessageHandler(
                ForwardToClientsMessageName,
                ForwardNamedMessageToClients);

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ForwardToClientMessageName);
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

        public void ClearNamedMessageHandlers()
        {
            if (networkManager.CustomMessagingManager != null)
            {
                foreach (KeyValuePair<string,NamedMessageHandlerDelegator> entry in messageNameToHandleNamedMessageHelper)
                {
                    networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(entry.Key);
                }
            }
            messageNameToHandleNamedMessageHelper.Clear();
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

            Log.Verbose(() => $"Sending message {messageName} to Netcode clients {targetNetcodeClientIds.JoinWith(", ")}");

            if (networkManager.IsServer)
            {
                SendNamedMessage(
                    messageName,
                    targetNetcodeClientIds,
                    fastBufferWriter,
                    networkDelivery);
            }
            else if (networkManager.IsClient)
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

            if (networkManager.IsServer
                || targetNetcodeClientId == NetworkManager.ServerClientId)
            {
                SendNamedMessage(
                    messageName,
                    targetNetcodeClientId,
                    fastBufferWriter,
                    networkDelivery);
            }
            else if (networkManager.IsClient)
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
            SendNamedMessage(
                messageName,
                NetworkManager.ServerClientId,
                fastBufferWriter,
                networkDelivery);
        }

        private void SendNamedMessage(
            string messageName,
            ulong clientId,
            FastBufferWriter fastBufferWriter,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            networkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                clientId,
                fastBufferWriter,
                networkDelivery);
        }

        private void SendNamedMessage(
            string messageName,
            IReadOnlyList<ulong> clientIds,
            FastBufferWriter fastBufferWriter,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            networkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                clientIds,
                fastBufferWriter,
                networkDelivery);
        }

        public IDisposable RegisterNamedMessageHandler(
            string messageName,
            Action<NamedMessage> handleMessage)
        {
            if (!messageNameToHandleNamedMessageHelper.TryGetValue(messageName, out NamedMessageHandlerDelegator namedMessageHandlerDelegator))
            {
                namedMessageHandlerDelegator = new NamedMessageHandlerDelegator(messageName);
                messageNameToHandleNamedMessageHelper[messageName] = namedMessageHandlerDelegator;
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, namedMessageHandlerDelegator.HandleNamedMessage);
            }

            NamedMessageHandler namedMessageHandler = new NamedMessageHandler(handleMessage);
            namedMessageHandlerDelegator.Add(namedMessageHandler);

            Log.Debug(() => $"RegisterNamedMessageHandler - new count of handlers for message name '{messageName}': {namedMessageHandlerDelegator.Count}");

            bool isDisposed = false;
            return Disposable.Create(() =>
            {
                if (isDisposed)
                {
                    return;
                }
                isDisposed = true;

                namedMessageHandlerDelegator.Remove(namedMessageHandler);
                if (namedMessageHandlerDelegator.Count <= 0)
                {
                    messageNameToHandleNamedMessageHelper.Remove(messageName);
                }
                Log.Debug(() => $"RemoveNamedMessageHandler - new count of handlers for message name '{messageName}': {namedMessageHandlerDelegator.Count}");
            });
        }
    }
}
