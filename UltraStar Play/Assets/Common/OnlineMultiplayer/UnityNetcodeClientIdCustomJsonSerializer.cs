using System;
using FullSerializer;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    // TODO: Common UInt64Converter for UnityNetcodeClientId and SteamId and ulong
    public class UnityNetcodeClientIdJsonSerializer : fsConverter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            JsonConverter.AddCustomConverter(() => new UnityNetcodeClientIdJsonSerializer());
        }

        public override bool CanProcess(Type type)
        {
            return type == typeof(UnityNetcodeClientId);
        }

        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {
            if (instance is not UnityNetcodeClientId netcodeClientId)
            {
                throw new JsonConverterException($"FullSerializer Internal Error -- Unexpected serialization type {instance?.GetType()}");
            }

            // Serialize as string to make sure that all ulong values are preserved in JSON
            // (i.e. not rounded to double or cut off)
            serialized = new fsData(netcodeClientId.Value.ToString());
            return fsResult.Success;
        }

        public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
        {
            if (storageType != typeof(UnityNetcodeClientId))
            {
                throw new JsonConverterException($"FullSerializer Internal Error -- Unexpected deserialization type {storageType}");
            }

            ulong rawValue = ulong.Parse(data.AsString);
            instance = new UnityNetcodeClientId(rawValue);
            return fsResult.Success;
        }
    }
}
