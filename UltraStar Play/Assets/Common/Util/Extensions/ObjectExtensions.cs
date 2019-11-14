
using System;

public static class ObjectExtensions
{

    // Performs the action on the object if the object is not null. Otherwise does nothing.
    public static void IfNotNull<T>(this T obj, Action<T> action) where T : class
    {
        if (obj != null)
        {
            action(obj);
        }
    }

    /// Returns the fallback object if obj is null.
    public static T OrIfNull<T>(this T obj, T fallbackObject)
    {
        if (obj == null)
        {
            return fallbackObject;
        }
        else
        {
            return obj;
        }
    }

    /// Returns true iff the value is one of the specified values.
    public static bool IsOneOf<T>(T value, params T[] values)
    {
        foreach (T v in values)
        {
            if (value.Equals(v))
            {
                return true;
            }
        }

        return false;
    }

    /// Returns true iff the value is one of the specified values.
    public static bool IsOneOf(this ValueType value, params ValueType[] values)
    {
        foreach (ValueType v in values)
        {
            if (value.Equals(v))
            {
                return true;
            }
        }

        return false;
    }
}