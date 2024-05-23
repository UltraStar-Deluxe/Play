using System;
using System.Collections.Generic;
using UniRx;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public class DelayedMessagingControl : IMessagingControl
    {
        private readonly MessagingControl messagingControl;

        public int DelayInMillis { get; set; }

        public DelayedMessagingControl(MessagingControl messagingControl)
        {
            this.messagingControl = messagingControl;
        }

        private void DelayAction(Action action)
        {
            if (DelayInMillis <= 0)
            {
                action();
                return;
            }

            int sleepTimeInMillis = RandomUtils.Range(1, DelayInMillis);
            float sleepTimeInSeconds = sleepTimeInMillis / 1000f;
            Log.Verbose(() => $"Delaying message by {sleepTimeInMillis} ms");
            MainThreadDispatcher.StartCoroutine(
            CoroutineUtils.ExecuteAfterDelayInSeconds(sleepTimeInSeconds, action));
        }

        public void RegisterNamedMessageHandlersToForwardMessagesIfNeeded()
        {
            messagingControl.RegisterNamedMessageHandlersToForwardMessagesIfNeeded();
        }

        public IDisposable RegisterNamedMessageHandler(string messageName, Action<NamedMessage> handleMessage)
        {
            return messagingControl.RegisterNamedMessageHandler(messageName, handleMessage);
        }

        public void SendNamedMessageToClients(string messageName, FastBufferWriter fastBufferWriter,
            IReadOnlyList<ulong> targetNetcodeClientIds, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            DelayAction(() => messagingControl.SendNamedMessageToClients(messageName, fastBufferWriter, targetNetcodeClientIds, networkDelivery));
        }

        public void SendNamedMessageToClient(string messageName, FastBufferWriter fastBufferWriter, ulong targetNetcodeClientId,
            NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced)
        {
            DelayAction(() => messagingControl.SendNamedMessageToClient(messageName, fastBufferWriter, targetNetcodeClientId, networkDelivery));
        }
    }
}
