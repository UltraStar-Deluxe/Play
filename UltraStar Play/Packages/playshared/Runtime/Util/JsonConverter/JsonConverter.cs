using System;
using System.Collections.Generic;
using System.Linq;
using JsonNet.ContractResolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;

/**
 * Implements serialization / deserialization of JSON using the serialization lib Newtonsoft.Json (aka. Json.NET).
 */
public static class JsonConverter
{
    // Cannot clear this list in RuntimeInitializeLoadType.SubsystemRegistration because it would clear already added converters.
    // Instead, use a dictionary such that every converter is only once in the collection, even when in the Unity editor.
    private static readonly Dictionary<string, Newtonsoft.Json.JsonConverter> customConverters = new();
    public static IReadOnlyCollection<string> CustomConverterTypeNames => customConverters.Keys;

    private static readonly List<Newtonsoft.Json.JsonConverter> defaultConverters = new List<Newtonsoft.Json.JsonConverter>
    {
        new StringEnumDefaultValueFallbackConverter(),
        new Color32Converter(),
        new GradientConfigConverter(),
        new ReactivePropertyConverter(),
        new Vector2Converter(),
        new Vector2IntConverter(),
        new Vector3Converter(),
        new Vector3IntConverter(),
        new Vector4Converter(),
    };

    private static bool isInitialized;

    public static string Prettify(string json)
    {
        return JToken.Parse(json).ToString();
    }

    public static string ToJson<T>(T obj, bool prettyPrint = false)
    {
        InitIfNotDoneYet();

        Formatting formatting = prettyPrint
            ? Formatting.Indented
            : Formatting.None;
        string json = JsonConvert.SerializeObject(obj, formatting);
        return json;
    }

    public static object FromJson(string json, Type type)
    {
        InitIfNotDoneYet();

        return JsonConvert.DeserializeObject(json, type);
    }

    public static T FromJson<T>(string json) where T : new()
    {
        InitIfNotDoneYet();

        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void FillFromJson<T>(string json, T existingInstance)
    {
        InitIfNotDoneYet();

        JsonConvert.PopulateObject(json, existingInstance);
    }

    public static void FillFromJsonCopy<T>(string json, T existingInstance)
    {
        InitIfNotDoneYet();

        object copy = FromJson(json, existingInstance.GetType());
        PropertyUtils.CopyProperties(copy, existingInstance);
    }

    public static void AddConverter<T>(JsonConverter<T> converter)
    {
        Debug.Log($"Added JsonConverter {converter} for type {typeof(T)}");

        customConverters[typeof(T).FullName] = converter;

        Init();
    }

    private static void InitIfNotDoneYet()
    {
        if (isInitialized)
        {
            return;
        }

        Init();
    }

    private static void Init()
    {
        isInitialized = true;

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = defaultConverters.Union(customConverters.Values).ToList(),
            ContractResolver = new PrivateSetterAndCtorContractResolver()
        };
    }
}
