using System;
using System.Reflection;
using Newtonsoft.Json;
using UniRx;

public class ReactivePropertyConverter : Newtonsoft.Json.JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

         // Get the value of the ReactiveProperty via reflection
         PropertyInfo propertyInfo = value.GetType().GetProperty("Value");
         object currentValue = propertyInfo.GetMethod.Invoke(value, null);
         if (currentValue == null)
         {
             writer.WriteNull();
             return;
         }

         serializer.Serialize(writer, currentValue);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object value, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return null;
        }

        Type deserializedValueType = objectType.GetGenericArguments()[0];
        object deserializedValue = serializer.Deserialize(reader, deserializedValueType);;

        // Set the value of the ReactiveProperty via reflection
        PropertyInfo propertyInfo = value.GetType().GetProperty("Value");
        propertyInfo.SetMethod.Invoke(value, new object[] { deserializedValue });
        return value;
    }

    public override bool CanConvert(Type type)
    {
        return type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(ReactiveProperty<>);
    }
}
