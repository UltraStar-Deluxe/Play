using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RandomUtils
{
    public static T RandomOfItems<T>(params T[] values)
    {
        return RandomOf(values.ToList());
    }
    
    public static T RandomOf<T>(IReadOnlyList<T> values)
    {
        if (values.IsNullOrEmpty())
        {
            return default(T);
        }
        int index = Random.Range(0, values.Count);
        return values[index];
    }

    public static HashSet<T> RandomHashSetOf<T>(IReadOnlyList<T> values)
    {
        HashSet<T> result = new();
        int itemCount = Random.Range(0, values.Count + 1);
        List<T> remainingValues = new(values);
        for (int i = 0; i < itemCount; i++)
        {
            T newValue = RandomOf(remainingValues);
            result.Add(newValue);
            remainingValues.Remove(newValue);
        }
        return result;
    }
}
