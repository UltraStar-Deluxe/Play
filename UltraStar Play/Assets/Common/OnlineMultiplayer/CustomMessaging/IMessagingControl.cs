using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public interface IMessagingControl
    {
        void RegisterNamedMessageHandlersToForwardMessages();

        public IDisposable RegisterNamedMessageHandler(
            string messageName,
            Action<NamedMessage> handleMessage);

        void ClearNamedMessageHandlers();

        void SendNamedMessageToClients(
            string messageName,
            FastBufferWriter fastBufferWriter,
            IReadOnlyList<ulong> targetNetcodeClientIds,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced);

        void SendNamedMessageToClient(
            string messageName,
            FastBufferWriter fastBufferWriter,
            ulong targetNetcodeClientId,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced);
    }
}
