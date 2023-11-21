using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public static class FastBufferReaderUtils
    {
        public static void ReadValuePacked(FastBufferReader fastBufferReader, out string text)
        {
            ByteUnpacker.ReadValuePacked(fastBufferReader, out text);
        }

        public static Action<ulong, FastBufferReader> CreateMessageHandlerCallback<T>(Action<ulong, T> handleMessage)
            where T : new()
        {
            return (ulong senderNetcodeClientId, FastBufferReader fastBufferReader) =>
            {
                ReadValuePacked(fastBufferReader, out string json);
                T dto = JsonConverter.FromJson<T>(json);
                handleMessage.Invoke(senderNetcodeClientId, dto);
            };
        }
    }
}
