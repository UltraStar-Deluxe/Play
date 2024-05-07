using System;
using System.Globalization;
using UnityEngine;

public static class NumberExtensions
{
    public static bool Equals(this float value, float targetValue, float tolerance)
    {
        return Mathf.Abs(value - targetValue) <= tolerance;
    }

    public static bool Equals(this double value, double targetValue, double tolerance)
    {
        return Math.Abs(value - targetValue) <= tolerance;
    }

    public static string ToStringInvariantCulture(this float value, string format=null)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string ToStringInvariantCulture(this double value, string format=null)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
