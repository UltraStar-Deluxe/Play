using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public struct NamedMessageHandler
    {
        public readonly Action<NamedMessage> handleMessage;

        public NamedMessageHandler(Action<NamedMessage> handleMessage)
        {
            this.handleMessage = handleMessage;
        }
    }
}
