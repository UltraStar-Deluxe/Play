using System.Collections.Generic;
using System.Data;
using System.Linq;

public static class DataReaderExtensions
{
    public static List<Dictionary<string, object>> ToDictionaryList(this IDataReader reader)
    {
        List<Dictionary<string, object>> result = new();
        while (reader.Read())
        {
            Dictionary<string,object> dictionary = Enumerable.Range(0, reader.FieldCount)
                .ToDictionary(reader.GetName, reader.GetValue);
            result.Add(dictionary);
        }
        return result;
    }

    public static List<T> ToList<T>(this IDataReader reader)
    {
        List<Dictionary<string,object>> dictionaryList = reader.ToDictionaryList();
        // Use JSON serialization to get the desired type
        string json = JsonConverter.ToJson(dictionaryList);
        return JsonConverter.FromJson<List<T>>(json);
    }
}
