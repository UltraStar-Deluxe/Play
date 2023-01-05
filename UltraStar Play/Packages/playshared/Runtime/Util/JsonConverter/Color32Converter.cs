using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

public class Color32Converter : fsConverter
{
    public override bool CanProcess(Type type)
    {
        return type == typeof(Color32);
    }

    public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
    {
        if (instance is not Color32 color)
        {
            throw new InvalidOperationException("FullSerializer Internal Error -- Unexpected serialization type");
        }

        string hexColor = color.a == 255
            ? ColorUtility.ToHtmlStringRGB(color)
            : ColorUtility.ToHtmlStringRGBA(color);
        serialized = new fsData($"#{hexColor}");
        return fsResult.Success;
    }

    public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
    {
        if (storageType != typeof(Color32))
        {
            throw new InvalidOperationException("FullSerializer Internal Error -- Unexpected deserialization type");
        }

        if (data.IsString == false)
        {
            return ParseColor32FromDataAsDictionary(data, ref instance);
        }

        return ParseColor32FromDataAsString(data, ref instance);
    }

    /**
     * Try parse string object in the form "#RRGGBBAA", where AA is optional.
     */
    private fsResult ParseColor32FromDataAsString(fsData data, ref object instance)
    {
        string dataAsString = data.AsString;
        if (Colors.TryParseHexColor(dataAsString, out Color32 outColor))
        {
            instance = outColor;
            return fsResult.Success;
        }
        return fsResult.Fail($"Unable to parse {data} into a Color32");
    }

    /**
     * Try parse object in the form {"r":255,"g":255,"g":255,"a":255}
     */
    private fsResult ParseColor32FromDataAsDictionary(fsData data, ref object instance)
    {
        Dictionary<string, fsData> dataAsDictionary = data.AsDictionary;

        byte GetColorFromDataAsDictionary(string key, byte fallbackValue)
        {
            if (dataAsDictionary.TryGetValue(key, out fsData colorData))
            {
                return (byte)colorData.AsInt64;
            }
            return fallbackValue;
        }

        try
        {
            byte r = GetColorFromDataAsDictionary("r", 255);
            byte g = GetColorFromDataAsDictionary("g", 255);
            byte b = GetColorFromDataAsDictionary("b", 255);
            byte a = GetColorFromDataAsDictionary("a", 255);
            instance = new Color32(r, g, b, a);
            return fsResult.Success;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            instance = Colors.white;
            return fsResult.Fail($"Unable to parse {data} into a Color32");
        }
    }
}
