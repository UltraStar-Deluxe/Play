using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public static T MostOccuringEntry<T>(IEnumerable<T> enumerable)
    {
        // See https://stackoverflow.com/questions/355945/find-the-most-occurring-number-in-a-listint
        T result = enumerable
            .GroupBy(entry => entry)
            .OrderByDescending(group => group.Count())
            .Select(grp => grp.Key)
            .First();
        return result;
    }

    /**
     * Calculates the intersection of two intervals A and B.
     */
    public static double[] GetIntersection(double aStart, double aEnd, double bStart, double bEnd)
    {
        if (aEnd < aStart
            || bEnd < bStart)
        {
            throw new IllegalArgumentException("'start' must be smaller than 'end'");
        }

        // https://scicomp.stackexchange.com/questions/26258/the-easiest-way-to-find-intersection-of-two-intervals
        if (bStart > aEnd
            || aStart > bEnd)
        {
            // no overlap
            return null;
        }

        double intersectionStart = Math.Max(aStart, bStart);
        double intersectionEnd = Math.Min(aEnd, bEnd);
        return new double[] { intersectionStart, intersectionEnd };
    }

    /**
     * Calculates the length of the intersection two intervals A and B.
     */
    public static double GetIntersectionLength(double aStart, double aEnd, double bStart, double bEnd)
    {
        double[] intersection = GetIntersection(aStart, aEnd, bStart, bEnd);
        if (intersection == null
            || intersection.Length < 2)
        {
            return -1;
        }

        return Math.Abs(intersection[1] - intersection[0]);
    }

    public static double GetIntersectionDistance(double aStart, double aEnd, double bStart, double bEnd)
    {
        if (GetIntersection(aStart, aEnd, bStart, bEnd) != null)
        {
            return 0;
        }

        return Math.Min(
            Math.Abs(bStart - aEnd),
            Math.Abs(aStart - bEnd));
    }

    public static float PercentToFactor(int zeroToHundred)
    {
        return zeroToHundred / 100f;
    }

    public static List<int> CreateIntList(int startValueInclusive, int endValueInclusive, int stepValue = 1)
    {
        if (stepValue <= 0)
        {
            throw new ArgumentException("Step value must be positive");
        }

        List<int> result = new();
        for (int i = startValueInclusive; i <= endValueInclusive; i += stepValue)
        {
            result.Add(i);
        }
        return result;
    }
    
    public static List<float> CreateFloatList(float startValueInclusive, float endValueInclusive, float stepValue = 1)
    {
        if (stepValue <= 0)
        {
            throw new ArgumentException("Step value must be positive");
        }

        List<float> result = new();
        for (float i = startValueInclusive; i <= endValueInclusive; i += stepValue)
        {
            result.Add(i);
        }
        return result;
    }

    public static int Towards(int current, int target, int step)
    {
        if (current == target
            || Mathf.Abs(current - target) < step)
        {
            return target;
        }
        
        if (current > target)
        {
            return current - step;
        }
        else
        {
            return current + step;
        }
    }
}
