using System;
using System.Globalization;

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

    public static Func<int> CreateIntGetterFromStringGetter(Func<string> valueGetter, int defaultValue = 0)
    {
        return () =>
        {
            string value = valueGetter();
            if (value.IsNullOrEmpty())
            {
                return defaultValue;
            }

            return int.TryParse(value, out int result)
                ? result
                : defaultValue;
        };
    }

    public static Func<float> CreateFloatGetterFromStringGetter(Func<string> valueGetter, float defaultValue = 0)
    {
        return () =>
        {
            string value = valueGetter();
            if (value.IsNullOrEmpty())
            {
                return defaultValue;
            }

            return float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result)
                ? result
                : defaultValue;
        };
    }

    public static bool TrySetIntFromString(string stringValue, Action<int> valueSetter)
    {
        if (int.TryParse(stringValue, out int intValue))
        {
            valueSetter(intValue);
            return true;
        }
        return false;
    }

    public static bool TrySetFloatFromString(string stringValue, Action<float> valueSetter)
    {
        if (float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue))
        {
            valueSetter(floatValue);
            return true;
        }
        return false;
    }

    public static float GetFloatFromString(string stringValue, float defaultValue)
    {
        if (float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue))
        {
            return floatValue;
        }
        return defaultValue;
    }
}
