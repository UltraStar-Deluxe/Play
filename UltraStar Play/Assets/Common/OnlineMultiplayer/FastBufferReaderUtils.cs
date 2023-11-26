using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public static class FastBufferReaderUtils
    {
        public static T ReadJsonValuePacked<T>(FastBufferReader fastBufferReader)
            where T : new()
        {
            string json = ReadValuePacked(fastBufferReader);
            return JsonConverter.FromJson<T>(json);
        }

        public static string ReadValuePacked(FastBufferReader fastBufferReader)
        {
            ByteUnpacker.ReadValuePacked(fastBufferReader, out string text);
            return text;
        }

        public static Action<NamedMessage> CreateMessageHandlerCallback<T>(Action<ulong, T> handleMessage)
            where T : new()
        {
            return (NamedMessage response) =>
            {
                T dto = ReadJsonValuePacked<T>(response.MessagePayload);
                handleMessage.Invoke(response.SenderNetcodeClientId, dto);
            };
        }
    }
}
