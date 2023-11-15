using System;
using System.Collections.Generic;
using System.Linq;
using FullSerializer;

// Implements serialization / deserialization of JSON using
// the serialization lib FullSerializer.
public static class JsonConverter
{
    // Indentation for pretty printing JSON.
    private const string IndentString = "    ";

    // Cannot clear this list in RuntimeInitializeLoadType.SubsystemRegistration because it would clear already added converters.
    // Instead, use a dictionary such that every converter is only once in the collection, even when in the Unity editor.
    private static readonly Dictionary<string, Func<fsBaseConverter>> customConverterProviders = new();
    public static IReadOnlyCollection<string> CustomConverterTypeNames => customConverterProviders.Keys;

    private static fsSerializer CreateSerializer()
    {
        fsSerializer newSerializer = new();
        newSerializer.AddConverter(new EnumDefaultValueFallbackConverter());
        newSerializer.AddConverter(new Color32Converter());
        newSerializer.AddConverter(new GradientConfigConverter());
        newSerializer.AddConverter(new ReactivePropertyConverter());
        customConverterProviders.Values.ForEach(customConverterProvider => newSerializer.AddConverter(customConverterProvider.Invoke()));
        return newSerializer;
    }

    public static string ToJson<T>(T obj, bool prettyPrint = false)
    {
        CreateSerializer()
            .TrySerialize(typeof(T), obj, out fsData data)
            .AssertSuccessWithoutWarnings();
        string json = fsJsonPrinter.CompressedJson(data);
        if (prettyPrint)
        {
            json = FormatJson(json);
        }
        return json;
    }

    public static object FromJson(string json, Type type, bool assertSuccessWithoutWarnings = true)
    {
        fsData data = fsJsonParser.Parse(json);
        object deserialized = new();
        fsResult tryDeserialize = CreateSerializer()
            .TryDeserialize(data, type, ref deserialized);
        if (assertSuccessWithoutWarnings)
        {
            tryDeserialize.AssertSuccessWithoutWarnings();
        }
        return deserialized;
    }

    public static T FromJson<T>(string json, bool assertSuccessWithoutWarnings = true) where T : new()
    {
        return (T)FromJson(json, typeof(T), assertSuccessWithoutWarnings);
    }

    public static void FillFromJson<T>(string json, T existingInstance, bool assertSuccessWithoutWarnings = true)
    {
        fsData data = fsJsonParser.Parse(json);
        fsResult fsResult = CreateSerializer()
            .TryDeserialize<T>(data, ref existingInstance);

        if (assertSuccessWithoutWarnings)
        {
            fsResult.AssertSuccessWithoutWarnings();
        }
    }

    public static void FillFromJsonCopy<T>(string json, T existingInstance, bool assertSuccessWithoutWarnings = true)
    {
        object loadedModSettings = FromJson(json, existingInstance.GetType(), false);
        PropertyUtils.CopyProperties(loadedModSettings, existingInstance);
    }

    // https://stackoverflow.com/questions/4580397/json-formatter-in-c
    private static string FormatJson(string json)
    {
        int indentation = 0;
        int quoteCount = 0;
        var result =
            from ch in json
            let quotes = ch == '"' ? quoteCount++ : quoteCount
            let lineBreak = ch == ',' && quotes % 2 == 0 ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(IndentString, indentation)) : null
            let openChar = ch == '{' || ch == '[' ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(IndentString, ++indentation)) : ch.ToString()
            let closeChar = ch == '}' || ch == ']' ? Environment.NewLine + String.Concat(Enumerable.Repeat(IndentString, --indentation)) + ch : ch.ToString()
            select lineBreak == null
                        ? openChar.Length > 1
                            ? openChar
                            : closeChar
                        : lineBreak;

        return String.Concat(result);
    }

    public static void AddCustomConverter<T>(Func<T> customConverterProvider) where T : fsBaseConverter
    {
        string converterTypeName = typeof(T).FullName;
        customConverterProviders[converterTypeName] = customConverterProvider;
    }
}
