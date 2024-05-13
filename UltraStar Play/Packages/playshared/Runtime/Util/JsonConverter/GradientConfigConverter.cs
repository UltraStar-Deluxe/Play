using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class GradientConfigConverter : JsonConverter<GradientConfig>
{
    public override void WriteJson(JsonWriter writer, GradientConfig value, JsonSerializer serializer)
    {
        writer.WriteValue($"{GradientConfigUtils.ToCssSyntax(value)}");
    }

    public override GradientConfig ReadJson(JsonReader reader, Type objectType, GradientConfig existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType is JsonToken.String)
        {
            // Try parse string object in the form "linear-gradient(angle, startColor, endColor)", where angle is optional.
            string dataAsString = (string)reader.Value;
            return GradientConfigUtils.FromCssSyntax(dataAsString);
        }

        try
        {
            // Try parse object in the form {"startColor":"#RRGGBBAA","endColor":"#RRGGBBAA", "angleDegrees":"360"}
            Dictionary<string, object> dataAsDictionary = serializer.Deserialize<Dictionary<string, object>>(reader);
            Color32 startColor = GetColor(dataAsDictionary, "startColor", Colors.clearBlack);
            Color32 endColor = GetColor(dataAsDictionary, "endColor", Colors.clearBlack);
            float angleDegrees = GetAngleInDegrees(dataAsDictionary, "angleDegrees", 0);
            return new GradientConfig(startColor, endColor, angleDegrees);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"ParseGradientConfigFromDataAsDictionary failed: {ex.Message}");
            return null;
        }
    }

    private static Color32 GetColor(Dictionary<string, object> dataAsDictionary, string key, Color32 fallbackValue)
    {
        if (dataAsDictionary.TryGetValue(key, out object colorData))
        {
            return Colors.CreateColor(Convert.ToString(colorData));
        }

        return fallbackValue;
    }

    private static float GetAngleInDegrees(Dictionary<string, object> dataAsDictionary, string key, float fallbackValue)
    {
        if (dataAsDictionary.TryGetValue(key, out object angleData))
        {
            return Convert.ToSingle(angleData);
        }

        return fallbackValue;
    }
}
