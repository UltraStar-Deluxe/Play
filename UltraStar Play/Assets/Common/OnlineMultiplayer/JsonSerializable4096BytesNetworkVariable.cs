using Unity.Collections;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public class JsonSerializable4096BytesNetworkVariable<T> : NetworkVariable<FixedString4096Bytes>
        where T : new()
    {
        private T lastDeserializedValue;
        private string lastDeserializedJsonString;

        public T DeserializedValue
        {
            get
            {
                if (Value.Value.IsNullOrEmpty())
                {
                    return default;
                }

                string currentJson = Value.Value;
                if (currentJson == lastDeserializedJsonString)
                {
                    return lastDeserializedValue;
                }

                lastDeserializedValue = JsonConverter.FromJson<T>(currentJson);
                lastDeserializedJsonString = currentJson;

                return lastDeserializedValue;
            }
            set
            {
                if (value == null)
                {
                    Value = null;
                    return;
                }

                string newJson = JsonConverter.ToJson(value);
                Value = newJson;
            }
        }
    }
}
