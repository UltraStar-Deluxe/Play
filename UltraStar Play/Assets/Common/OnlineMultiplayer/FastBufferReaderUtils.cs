using Unity.Collections;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public static class FastBufferReaderUtils
    {
        public static void ReadValuePacked(FastBufferReader fastBufferReader, out string text)
        {
            ByteUnpacker.ReadValuePacked(fastBufferReader, out text);
        }
    }
}
