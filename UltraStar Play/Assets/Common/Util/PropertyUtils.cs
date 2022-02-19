using System;
using System.Globalization;
using UnityEngine.UIElements;

public static class PropertyUtils
{
    public static Func<string> CreateStringGetterFromUintGetter(Func<uint> valueGetter, bool zeroToEmpty)
    {
        return () =>
        {
            uint value = valueGetter();
            if (zeroToEmpty && value == 0)
            {
                return "";
            }
            return value.ToString();
        };
    }

    public static Action<string> CreateStringSetterFromUintSetter(Action<uint> valueSetter)
    {
        return (newValue) =>
        {
            if (!newValue.IsNullOrEmpty()
                && uint.TryParse(newValue, out uint newValueUint))
            {
                valueSetter(newValueUint);
            }
            else
            {
                valueSetter(0);
            }
        };
    }

    public static Func<string> CreateStringGetterFromFloatGetter(Func<float> valueGetter, bool zeroToEmpty, string toStringFormat)
    {
        return () =>
        {
            float value = valueGetter();
            if (zeroToEmpty
                && value == 0)
            {
                return "";
            }
            return value.ToString(null, CultureInfo.InvariantCulture);
        };
    }

    public static Action<string> CreateStringSetterFromFloatSetter(Action<float> valueSetter)
    {
        return (newValue) =>
        {
            if (!newValue.IsNullOrEmpty()
                && float.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float newValueFloat))
            {
                valueSetter(newValueFloat);
            }
            else
            {
                valueSetter(0);
            }
        };
    }
}
