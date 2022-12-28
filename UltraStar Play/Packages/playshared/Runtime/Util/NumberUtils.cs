using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class NumberUtils
{
    public static int Limit(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }

    public static float Limit(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }

    public static double Limit(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }

    // Modulus operation that wraps negative numbers.
    // Example: Mod(-1, 3) == 2
    public static int Mod(int a, int n)
    {
        int result = a % n;
        if ((result < 0 && n > 0) || (result > 0 && n < 0))
        {
            result += n;
        }
        return result;
    }

    public static T Median<T>(IEnumerable<T> enumerable)
    {
        List<T> list = enumerable.ToList();
        if (list.IsNullOrEmpty())
        {
            return default(T);
        }

        list.Sort();
        return list[list.Count / 2];
    }
}
