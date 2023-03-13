using System;
using System.Linq;
using FullSerializer;

// Implements serialization / deserialization of JSON using
// the serialization lib FullSerializer.
public static class JsonConverter
{
    // Indentation for pretty printing JSON.
    private const string IndentString = "    ";

    private static fsSerializer CreateSerializer()
    {
        fsSerializer newSerializer = new();
        newSerializer.AddConverter(new Color32Converter());
        newSerializer.AddConverter(new GradientConfigConverter());
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

    public static T FromJson<T>(string json, bool assertSuccessWithoutWarnings = true) where T : new()
    {
        fsData data = fsJsonParser.Parse(json);
        T deserialized = new();
        fsResult tryDeserialize = CreateSerializer()
            .TryDeserialize<T>(data, ref deserialized);
        if (assertSuccessWithoutWarnings)
        {
            tryDeserialize.AssertSuccessWithoutWarnings();
        }
        return deserialized;
    }

    public static void FillFromJson<T>(string json, T existingInstance)
    {
        fsData data = fsJsonParser.Parse(json);
        CreateSerializer().TryDeserialize<T>(data, ref existingInstance).AssertSuccessWithoutWarnings();
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
}
