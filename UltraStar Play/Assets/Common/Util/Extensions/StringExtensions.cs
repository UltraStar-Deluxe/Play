using System;
using UnityEngine;

public static class StringExtensions
{
    public static int TryParseAsInteger(this string text, int fallbackValue)
    {
        try
        {
            return int.Parse(text);
        }
        catch (FormatException)
        {
            Debug.Log("Cannot parse '" + text + "' as int. Using fallback value " + fallbackValue + " instead.");
            return fallbackValue;
        }
    }
}