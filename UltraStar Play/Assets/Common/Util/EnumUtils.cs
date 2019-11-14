using System;
using System.Collections.Generic;

public static class EnumUtils
{
    public static List<T> GetValuesAsList<T>()
    {
        System.Array enumValues = Enum.GetValues(typeof(T));
        List<T> result = new List<T>();
        foreach (object o in enumValues)
        {
            if (o is T)
            {
                result.Add((T)o);
            }
        }
        return result;
    }
}