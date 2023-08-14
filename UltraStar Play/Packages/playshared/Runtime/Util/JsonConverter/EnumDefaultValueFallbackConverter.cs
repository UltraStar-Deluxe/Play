using System;
using FullSerializer;
using NUnit.Framework.Constraints;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

/**
 * Converts a string to an enum value if possible.
 * Uses the Enum's default value as fallback.
 */
public class EnumDefaultValueFallbackConverter : fsConverter
{
    private const bool IgnoreCase = true;

    public override bool CanProcess(Type type)
    {
        return type.IsEnum;
    }

    public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
    {
        if (!storageType.IsEnum)
        {
            throw new InvalidOperationException($"EnumDefaultValueFallbackConverter.TrySerialize -- Storage type {storageType} is not an enum");
        }

        Enum instanceAsEnum = instance as Enum;
        string instanceAsString = instanceAsEnum != null ? instanceAsEnum.ToString() : "";
        serialized = new fsData(instanceAsString);
        return fsResult.Success;
    }

    public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
    {
        if (!storageType.IsEnum)
        {
            throw new InvalidOperationException($"EnumDefaultValueFallbackConverter.TryDeserialize -- Storage type {storageType} is not an enum");
        }

        return ParseEnumFromDataAsString(data, ref instance, storageType);
    }

    private fsResult ParseEnumFromDataAsString(fsData data, ref object instance, Type storageType)
    {
        string dataAsString = data.AsString;
        if (Enum.TryParse(storageType, dataAsString, IgnoreCase, out object parsedValue))
        {
            instance = parsedValue;
            return fsResult.Success;
        }

        Array enumValues = Enum.GetValues(storageType);
        if (enumValues.Length <= 0)
        {
            throw new JsonException($"Cannot deserialize enum, no enum values for type {storageType}");
        }

        Enum fallbackValue = enumValues.GetValue(0) as Enum;
        Debug.LogWarning($"Unable to parse '{data}' into an Enum of type {storageType}, using fallback value {fallbackValue} instead");
        instance = fallbackValue;
        return fsResult.Success;
    }
}
