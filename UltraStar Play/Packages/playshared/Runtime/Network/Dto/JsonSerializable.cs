/**
 * Marker interface that an object can be serialized to JSON (via extension method)
 */
public interface JsonSerializable
{
}

public static class JsonSerializableExtensions
{
    public static string ToJson(this JsonSerializable jsonSerializable)
    {
        return JsonConverter.ToJson(jsonSerializable);
    }
}
