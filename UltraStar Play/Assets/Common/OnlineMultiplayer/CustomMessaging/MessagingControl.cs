using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
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

        private NetworkManager NetworkManager => NetworkManager.Singleton;
        private readonly Func<IReadOnlyList<ulong>> otherLobbyMemberUnityNetcodeClientIdsGetter;

        private bool hasRegisteredForwardNamedMessageHandlers;

        public MessagingControl(Func<IReadOnlyList<ulong>> otherLobbyMemberUnityNetcodeClientIdsGetter)
        {
            this.otherLobbyMemberUnityNetcodeClientIdsGetter = otherLobbyMemberUnityNetcodeClientIdsGetter;
        }

        public void RegisterForwardNamedMessageHandlersIfNeeded()
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

        private void ForwardNamedMessageToClient(ulong senderNetcodeClientId, FastBufferReader fastBufferReader)
        {
            Debug.Log($"Forwarding named message from client {senderNetcodeClientId} to single client");

            fastBufferReader.ReadValueSafe(out string messageName);
            fastBufferReader.ReadValueSafe(out ulong targetNetcodeClientId);
            fastBufferReader.ReadValueSafe(out NetworkDelivery networkDelivery);
            int originalMessageLength = fastBufferReader.Length - fastBufferReader.Position;
            byte[] originalMessageBytes = new byte[originalMessageLength];
            fastBufferReader.ReadBytes(ref originalMessageBytes, originalMessageBytes.Length, 0);

            using FastBufferWriter originalMessageWriter = new();
            originalMessageWriter.WriteBytes(originalMessageBytes);
            SendNamedMessageToClient(
                messageName,
                originalMessageWriter,
                targetNetcodeClientId,
                networkDelivery);
        }

        private void ForwardNamedMessageToClients(ulong senderNetcodeClientId, FastBufferReader fastBufferReader)
        {
            Debug.Log($"Forwarding named message from client {senderNetcodeClientId} to multiple clients");

            fastBufferReader.ReadValueSafe(out string messageName);
            fastBufferReader.ReadValueSafe(out ulong[] targetNetcodeClientIds);
            fastBufferReader.ReadValueSafe(out NetworkDelivery networkDelivery);
            int originalMessageLength = fastBufferReader.Length - fastBufferReader.Position;
            byte[] originalMessageBytes = new byte[originalMessageLength];
            fastBufferReader.ReadBytes(ref originalMessageBytes, originalMessageBytes.Length, 0);

            using FastBufferWriter originalMessageWriter = new();
            originalMessageWriter.WriteBytes(originalMessageBytes);
            SendNamedMessageToClients(
                messageName,
                originalMessageWriter,
                targetNetcodeClientIds,
                networkDelivery);
        }

        public void SendNamedMessageToAllClients(
            string messageName,
            FastBufferWriter fastBufferWriter,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            NetworkManager.CustomMessagingManager.SendNamedMessageToAll(
                messageName,
                fastBufferWriter,
                networkDelivery);
        }

        public void SendNamedMessageToOtherClients(
            string messageName,
            FastBufferWriter fastBufferWriter,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            SendNamedMessageToClients(
                messageName,
                fastBufferWriter,
                otherLobbyMemberUnityNetcodeClientIdsGetter.Invoke(),
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
                using FastBufferWriter forwardedFastBufferWriter = new();
                forwardedFastBufferWriter.WriteValueSafe(messageName);
                forwardedFastBufferWriter.WriteValueSafe(targetNetcodeClientIds.ToArray());
                forwardedFastBufferWriter.WriteValueSafe(networkDelivery);
                forwardedFastBufferWriter.CopyFrom(fastBufferWriter);
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
                using FastBufferWriter forwardedFastBufferWriter = new();
                forwardedFastBufferWriter.WriteValueSafe(messageName);
                forwardedFastBufferWriter.WriteValueSafe(targetNetcodeClientId);
                forwardedFastBufferWriter.CopyFrom(fastBufferWriter);
                SendNamedMessageToServer(
                    "FORWARD_TO_CLIENT",
                    forwardedFastBufferWriter,
                    networkDelivery);
            }

            fastBufferWriter.Dispose();
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
            Action<ulong, FastBufferReader> handleMessage)
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

            return Disposable.Create(() =>
            {
                Debug.Log($"RemoveNamedMessageHandler - new count of handlers for message name '{messageName}': {messageHandlers.Count}");
                messageHandlers.Remove(namedMessageHandler);
            });
        }
    }
}
