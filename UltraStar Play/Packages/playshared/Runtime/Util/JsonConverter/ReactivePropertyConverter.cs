using System;
using System.Collections.Generic;
using System.Reflection;
using FullSerializer;
using UniRx;

public class ReactivePropertyConverter : fsConverter
{
    private static readonly List<Type> supportedTypes = new List<Type>
    {
        typeof(IReactiveProperty<string>),
    };

    public override bool CanProcess(Type type)
    {
        return type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(ReactiveProperty<>);
    }

    public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
    {
        // Get the value via reflection
        PropertyInfo propertyInfo = instance.GetType().GetProperty("Value");
        object currentValue = propertyInfo.GetMethod.Invoke(instance, null);

        switch (currentValue)
        {
            case string s:
                serialized = new fsData(s);
                break;
            case bool b:
                serialized = new fsData(b);
                break;
            case double d:
                serialized = new fsData(d);
                break;
            case long l:
                serialized = new fsData(l);
                break;
            case Enum e:
                serialized = new fsData(e.ToString());
                break;
            default:
                Type serializedValueType = storageType.GetGenericArguments()[0];
                Serializer.TrySerialize(serializedValueType, currentValue, out serialized);
                break;
        }
        return fsResult.Success;
    }

    public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
    {
        Type deserializedValueType = storageType.GetGenericArguments()[0];
        object deserializedValue = null;
        if (data.IsString)
        {
            if (deserializedValueType.IsEnum)
            {
                deserializedValue = Enum.Parse(deserializedValueType, data.AsString);
            }
            else
            {
                deserializedValue = data.AsString;
            }
        }
        else if (data.IsBool)
        {
            deserializedValue = data.AsBool;
        }
        else if (data.IsDouble)
        {
            if (deserializedValueType == typeof(float))
            {
                deserializedValue = (float)data.AsDouble;
            }
            else
            {
                deserializedValue = data.AsDouble;
            }
        }
        else if (data.IsInt64)
        {
            if (deserializedValueType == typeof(int))
            {
                deserializedValue = (int)data.AsInt64;
            }
            else
            {
                deserializedValue = data.AsInt64;
            }
        }
        else if (data.IsList)
        {
            List<fsData> dataAsList = data.AsList;
            if (dataAsList != null)
            {
                Serializer.TryDeserialize(data, deserializedValueType, ref deserializedValue);
            }
        }
        else if (data.IsDictionary)
        {
            Dictionary<string, fsData> dataAsDictionary = data.AsDictionary;
            if (dataAsDictionary != null)
            {
                Serializer.TryDeserialize(data, deserializedValueType, ref deserializedValue);
            }
        }

        if (instance == null)
        {
            throw new JsonConverterException("Cannot deserialize, instance is null");
        }

        // Set the value via reflection
        PropertyInfo propertyInfo = instance.GetType().GetProperty("Value");
        propertyInfo.SetMethod.Invoke(instance, new object[] { deserializedValue });
        return fsResult.Success;
    }
}
