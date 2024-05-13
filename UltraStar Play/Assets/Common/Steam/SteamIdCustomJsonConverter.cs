using System;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    // TODO: Common UInt64Converter for UnityNetcodeClientId and SteamId and ulong
    public class SteamIdCustomJsonConverter : JsonConverter<SteamId>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            JsonConverter.AddConverter(new SteamIdCustomJsonConverter());
        }

        public override void WriteJson(JsonWriter writer, SteamId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value.ToString());
        }

        public override SteamId ReadJson(JsonReader reader, Type objectType, SteamId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string value = (string)reader.Value;
            return (SteamId)ulong.Parse(value);
        }
    }
}
