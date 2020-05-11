
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

    /// Returns true iff the value is equal to one of the specified values.
    /// Thereby, comparison is done using object.Equals.
    public static bool IsOneOf<T>(this T value, params T[] values)
    {
        foreach (T v in values)
        {
            if (object.Equals(value, v))
            {
                return true;
            }
        }

        return false;
    }
}
