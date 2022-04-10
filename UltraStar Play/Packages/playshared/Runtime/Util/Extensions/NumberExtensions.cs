using System;
using UnityEngine;

public static class NumberExtensions
{
    public static bool NearlyEquals(this float value, float targetValue, float tolerance)
    {
        return Mathf.Abs(value - targetValue) <= tolerance;
    }
    
    public static bool NearlyEquals(this double value, double targetValue, double tolerance)
    {
        return Math.Abs(value - targetValue) <= tolerance;
    }
}
