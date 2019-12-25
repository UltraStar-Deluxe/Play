using System;

public static class StringExtensions
{
    public static int TryParseAsInteger(this string newText, int fallbackValue)
    {
        try
        {
            return int.Parse(newText);
        }
        catch (FormatException)
        {
            return fallbackValue;
        }
    }
}