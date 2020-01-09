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

    // Implements string.IsNullOrEmpty(...) as Extension Method.
    // This way it can be called as myString.IsNullOrEmpty(); instead string.IsNullOrEmpty(myString);
    public static bool IsNullOrEmpty(this string txt)
    {
        return string.IsNullOrEmpty(txt);
    }
}