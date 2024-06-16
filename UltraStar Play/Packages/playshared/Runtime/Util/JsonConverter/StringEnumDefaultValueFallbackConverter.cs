using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/**
 * Converts a string to an enum value if possible.
 * Uses the Enum's default value as fallback.
 */
public class StringEnumDefaultValueFallbackConverter : StringEnumConverter
{
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        catch
        {
            return Enum.Parse(objectType, "0");
        }
    }
}
