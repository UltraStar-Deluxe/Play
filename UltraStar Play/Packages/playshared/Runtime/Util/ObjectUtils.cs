using System;

public static class ObjectUtils
{
    // Swaps the values of a and b.
    // Example: Let a=1 and b=2. Then Swap(ref a, ref b) will result in a=2 and b=1.
    public static void Swap<T>(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }
    
    public static T FirstNonDefault<T>(params T[] items)
    {
        foreach (T item in items)
        {
            if (!Equals(item, default(T)))
            {
                return item;
            }
        }

        return default(T);
    }
    
    public static void AssertNotNull(object nullableReference, string paramName)
    {
        if (nullableReference == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static string NullableToString(object obj, string nullValueResult)
    {
        if (obj == null)
        {
            return nullValueResult;
        }
        else
        {
            return obj.ToString();
        }
    }
}
