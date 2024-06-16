using System;
using System.Collections.Generic;
using UniRx;
using Unity.Collections;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public class DelayedMessagingControl : IMessagingControl
    {
        private readonly IMessagingControl messagingControl;

        public int DelayInMillis { get; set; }

        public DelayedMessagingControl(IMessagingControl messagingControl)
        {
            this.messagingControl = messagingControl;
        }

        private void DelayMessage(string messageName, IReadOnlyList<ulong> targetNetcodeClientIds, Action action)
        {
            if (DelayInMillis <= 0)
            {
                action();
                return;
            }

            int sleepTimeInMillis = RandomUtils.Range(1, DelayInMillis);
            float sleepTimeInSeconds = sleepTimeInMillis / 1000f;
            Log.Verbose(() => $"Delaying message '{messageName}' to Netcode clients {targetNetcodeClientIds.JoinWith(", ")} by {sleepTimeInSeconds:F3} seconds");
            MainThreadDispatcher.StartCoroutine(
            CoroutineUtils.ExecuteAfterDelayInSeconds(sleepTimeInSeconds, action));
        }

        public void RegisterNamedMessageHandlersToForwardMessages()
        {
            messagingControl.RegisterNamedMessageHandlersToForwardMessages();
        }

        public IDisposable RegisterNamedMessageHandler(string messageName, Action<NamedMessage> handleMessage)
        {
            return messagingControl.RegisterNamedMessageHandler(messageName, handleMessage);
        }

        public void ClearNamedMessageHandlers()
        {
            messagingControl.ClearNamedMessageHandlers();
        }

        public void SendNamedMessageToClients(
            string messageName,
            FastBufferWriter fastBufferWriter,
            IReadOnlyList<ulong> targetNetcodeClientIds,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            if (DelayInMillis <= 0)
            {
                messagingControl.SendNamedMessageToClients(messageName, fastBufferWriter, targetNetcodeClientIds, networkDelivery);
                return;
            }

            FastBufferWriter fastBufferWriterCopy = CopyPersistent(fastBufferWriter);
            DelayMessage(messageName, targetNetcodeClientIds,
                () => messagingControl.SendNamedMessageToClients(messageName, fastBufferWriterCopy, targetNetcodeClientIds, networkDelivery));
        }

        public void SendNamedMessageToClient(
            string messageName,
            FastBufferWriter fastBufferWriter,
            ulong targetNetcodeClientId,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            if (DelayInMillis <= 0)
            {
                messagingControl.SendNamedMessageToClient(messageName, fastBufferWriter, targetNetcodeClientId, networkDelivery);
                return;
            }

            FastBufferWriter fastBufferWriterCopy = CopyPersistent(fastBufferWriter);
            DelayMessage(messageName, new List<ulong>() {targetNetcodeClientId},
                () => messagingControl.SendNamedMessageToClient(messageName, fastBufferWriterCopy, targetNetcodeClientId, networkDelivery));
        }

        /**
         * The original FastBufferWriter will be disposed by calling code before sending the request with delay.
         * Thus, this method creates a copy that can be disposed later.
         */
        private FastBufferWriter CopyPersistent(FastBufferWriter original)
        {
            FastBufferWriter copy = new FastBufferWriter(original.Length, Allocator.Persistent);
            copy.TryBeginWrite(original.Length);
            copy.CopyFrom(original);
            return copy;
        }
    }
}
