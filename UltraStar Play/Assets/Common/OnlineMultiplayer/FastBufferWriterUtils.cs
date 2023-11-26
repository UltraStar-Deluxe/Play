using Unity.Collections;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public static class FastBufferWriterUtils
    {
        public static FastBufferWriter WriteJsonValuePacked(JsonSerializable jsonSerializable)
        {
            return WriteValuePacked(jsonSerializable.ToJson());
        }

        public static FastBufferWriter WriteValuePacked(string text)
        {
            FastBufferWriter w = new(1024, Allocator.Temp);
            BytePacker.WriteValuePacked(w, text);
            return w;
        }
    }
}
