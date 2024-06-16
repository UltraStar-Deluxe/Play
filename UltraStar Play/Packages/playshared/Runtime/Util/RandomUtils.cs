using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RandomUtils
{
    private static System.Random random = new();

    public static T RandomOfItems<T>(params T[] values)
    {
        if (values.IsNullOrEmpty())
        {
            return default(T);
        }
        return RandomOf(values.ToList());
    }

    public static bool RandomTrue(int probabilityOfTrueInPercent = 50)
    {
        // Random.Range: first parameter 0 is inclusive, second parameter 100 is exclusive
        return Random.Range(0, 100) < probabilityOfTrueInPercent;
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
        if (values.IsNullOrEmpty())
        {
            return new HashSet<T>();
        }
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

    public static Color32 RandomColor()
    {
        int r = Random.Range(0, 255);
        int g = Random.Range(0, 255);
        int b = Random.Range(0, 255);
        return new Color32((byte)r, (byte)g, (byte)b, 255);
    }

    /**
     * Generate random number. In contrast to Unity's method, this works also on other threads than the main thread.
     */
    public static int Range(int minValue, int maxValueExclusive)
    {
        return random.Next(minValue, maxValueExclusive);
    }
}
