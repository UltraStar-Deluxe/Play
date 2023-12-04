using System;
using Newtonsoft.Json;

// Implements serialization / deserialization of JSON using
// the serialization lib Newtonsoft.Json.
public static class NewtonsoftJsonConverter
{
    public static string ToJson<T>(T obj, bool prettyPrint = false)
    {
        Formatting formatting = prettyPrint
            ? Formatting.Indented
            : Formatting.None;
        string json = JsonConvert.SerializeObject(obj, formatting);
        return json;
    }

    public static object FromJson(string json, Type type)
    {
        return JsonConvert.DeserializeObject(json, type);
    }

    public static T FromJson<T>(string json) where T : new()
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void FillFromJson<T>(string json, T existingInstance)
    {
        JsonConvert.PopulateObject(json, existingInstance);
    }

    public static void FillFromJsonCopy<T>(string json, T existingInstance)
    {
        object copy = FromJson(json, existingInstance.GetType());
        PropertyUtils.CopyProperties(copy, existingInstance);
    }
}
