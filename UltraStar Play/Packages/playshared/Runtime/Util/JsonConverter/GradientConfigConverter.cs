using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

public class GradientConfigConverter  : fsConverter
{
    public override bool CanProcess(Type type)
    {
        return type == typeof(GradientConfig);
    }

    public override object CreateInstance(fsData data, Type storageType)
    {
        return null;
    }
    
    public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
    {
        if (instance is not GradientConfig gradientConfig)
        {
            throw new InvalidOperationException("FullSerializer Internal Error -- Unexpected serialization type");
        }

        serialized = new fsData($"{GradientConfigUtils.ToCssSyntax(gradientConfig)}");
        return fsResult.Success;
    }

    public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
    {
        if (storageType != typeof(GradientConfig))
        {
            throw new InvalidOperationException("FullSerializer Internal Error -- Unexpected deserialization type");
        }

        if (data.IsString == false)
        {
            return ParseGradientConfigFromDataAsDictionary(data, ref instance);
        }

        return ParseGradientConfigFromDataAsString(data, ref instance);
    }

    /**
     * Try parse string object in the form "linear-gradient(angle, startColor, endColor)", where angle is optional.
     */
    private fsResult ParseGradientConfigFromDataAsString(fsData data, ref object instance)
    {
        string dataAsString = data.AsString;
        try
        {
            instance = GradientConfigUtils.FromCssSyntax(dataAsString);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unable to parse {data} into a GradientConfig: {ex.Message}");
            // Ignore this issue
            return fsResult.Success;
        }

        if (instance != null)
        {
            return fsResult.Success;
        }

        return fsResult.Fail($"Unable to parse {data} into a GradientConfig");
    }

    /**
     * Try parse object in the form {"startColor":"#RRGGBBAA","endColor":"#RRGGBBAA", "angleDegrees":"360"}
     */
    private fsResult ParseGradientConfigFromDataAsDictionary(fsData data, ref object instance)
    {
        Dictionary<string, fsData> dataAsDictionary = data.AsDictionary;

        Color32 GetColorFromDataAsDictionary(string key, Color32 fallbackValue)
        {
            if (dataAsDictionary.TryGetValue(key, out fsData colorData))
            {
                return Colors.CreateColor(colorData.AsString);
            }
            return fallbackValue;
        }

        float GetAngleInDegreesFromDataAsDictionary(string key, float fallbackValue)
        {
            if (dataAsDictionary.TryGetValue(key, out fsData angleData))
            {
                return (float)angleData.AsDouble;
            }
            return fallbackValue;
        }
        
        try
        {
            Color32 startColor = GetColorFromDataAsDictionary("startColor", Colors.clearBlack);
            Color32 endColor = GetColorFromDataAsDictionary("endColor", Colors.clearBlack);
            float angleDegrees = GetAngleInDegreesFromDataAsDictionary("angleDegree", 0);
            instance = new GradientConfig(startColor, endColor, angleDegrees);
            return fsResult.Success;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            instance = Colors.white;
            return fsResult.Fail($"Unable to parse {data} into a GradientConfig");
        }
    }
}
