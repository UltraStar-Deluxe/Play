using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class YamlConverter
{
    public static T FromYaml<T>(string text)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        T instance = deserializer.Deserialize<T>(text);
        return instance;
    }
}
