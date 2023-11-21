using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public struct NamedMessageHandler
    {
        public readonly Action<ulong, FastBufferReader> handleMessage;

        public NamedMessageHandler(Action<ulong, FastBufferReader> handleMessage)
        {
            this.handleMessage = handleMessage;
        }
    }
}
