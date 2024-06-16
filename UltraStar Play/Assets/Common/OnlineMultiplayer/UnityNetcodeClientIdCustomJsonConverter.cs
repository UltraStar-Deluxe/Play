using System;
using Newtonsoft.Json;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    // TODO: Common UInt64Converter for UnityNetcodeClientId and SteamId and ulong
    public class UnityNetcodeClientIdJsonConverter : JsonConverter<UnityNetcodeClientId>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            JsonConverter.AddConverter(new UnityNetcodeClientIdJsonConverter());
        }

        public override void WriteJson(JsonWriter writer, UnityNetcodeClientId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value.ToString());
        }

        public override UnityNetcodeClientId ReadJson(JsonReader reader, Type objectType, UnityNetcodeClientId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string value = (string)reader.Value;
            return new UnityNetcodeClientId(ulong.Parse(value));
        }
    }
}
