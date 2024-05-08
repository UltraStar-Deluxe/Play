using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Color32Converter : JsonConverter<Color32>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        JsonConverter.AddConverter(new Color32Converter());
    }

    public override void WriteJson(JsonWriter writer, Color32 value, JsonSerializer serializer)
    {
        writer.WriteValue($"#{Colors.ToHexColor(value)}");
    }

    public override Color32 ReadJson(JsonReader reader, Type objectType, Color32 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return default;
        }

        if (reader.TokenType is JsonToken.String)
        {
            // Try parse string object in the form "#RRGGBBAA", where AA is optional.
            string dataAsString = (string)reader.Value;
            if (Colors.TryParseHexColor(dataAsString, out Color32 outColor))
            {
                return outColor;
            }
        }

        try
        {
            // Try parse object in the form {"r":255,"g":255,"g":255,"a":255}
            Dictionary<string, byte> dataAsDictionary = serializer.Deserialize<Dictionary<string, byte>>(reader);
            byte r = GetColorOrFallback(dataAsDictionary, "r", 0);
            byte g = GetColorOrFallback(dataAsDictionary, "g", 0);
            byte b = GetColorOrFallback(dataAsDictionary, "b", 0);
            byte a = GetColorOrFallback(dataAsDictionary, "a", 255);
            return new Color32(r, g, b, a);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to parse {nameof(Color32)}: {ex.Message}");
            return default;
        }
    }

    private static byte GetColorOrFallback(Dictionary<string, byte> dataAsDictionary, string key, byte fallbackValue)
    {
        return dataAsDictionary.TryGetValue(key, out byte colorValue)
            ? colorValue
            : fallbackValue;
    }
}
