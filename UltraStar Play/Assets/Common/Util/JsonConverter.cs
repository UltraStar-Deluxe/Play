using System;
using FullSerializer;
using System.Linq;

// Implements serialization / deserialization of JSON using
// the serialization lib FullSerializer.
public static class JsonConverter
{
    // Indentation for pretty printing JSON.
    private const string INDENT_STRING = "    ";

    private static readonly fsSerializer serializer = CreateSerializer();

    private static fsSerializer CreateSerializer()
    {
        fsSerializer newSerializer = new fsSerializer();
        return newSerializer;
    }

    public static string ToJson<T>(T obj)
    {
        return ToJson<T>(obj, false);
    }

    public static string ToJson<T>(T obj, bool prettyPrint)
    {
        serializer.TrySerialize(typeof(T), obj, out fsData data).AssertSuccessWithoutWarnings();
        string json = fsJsonPrinter.CompressedJson(data);
        if (prettyPrint)
        {
            json = FormatJson(json);
        }
        return json;
    }

    public static T FromJson<T>(string json) where T : new()
    {
        fsData data = fsJsonParser.Parse(json);
        T deserialized = new T();
        serializer.TryDeserialize<T>(data, ref deserialized).AssertSuccessWithoutWarnings();
        return deserialized;
    }

    // https://stackoverflow.com/questions/4580397/json-formatter-in-c
    private static string FormatJson(string json)
    {
        int indentation = 0;
        int quoteCount = 0;
        var result =
            from ch in json
            let quotes = ch == '"' ? quoteCount++ : quoteCount
            let lineBreak = ch == ',' && quotes % 2 == 0 ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, indentation)) : null
            let openChar = ch == '{' || ch == '[' ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, ++indentation)) : ch.ToString()
            let closeChar = ch == '}' || ch == ']' ? Environment.NewLine + String.Concat(Enumerable.Repeat(INDENT_STRING, --indentation)) + ch : ch.ToString()
            select lineBreak == null
                        ? openChar.Length > 1
                            ? openChar
                            : closeChar
                        : lineBreak;

        return String.Concat(result);
    }
}
