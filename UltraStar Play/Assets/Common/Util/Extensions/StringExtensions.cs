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

    // Removes opening and ending part from a string.
    public static string Strip(this string txt, string opening, string ending)
    {
        if (txt.Length >= (opening.Length + ending.Length)
            && txt.StartsWith(opening)
            && txt.EndsWith(ending))
        {
            return txt.Substring(opening.Length, txt.Length - (opening.Length + ending.Length));
        }
        return txt;
    }
}
