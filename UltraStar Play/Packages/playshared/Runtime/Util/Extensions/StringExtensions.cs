using System;
using System.Text;
using System.Text.RegularExpressions;
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

    public static string TrimEnd(this string txt, string prefix)
    {
        if (txt.EndsWith(prefix))
        {
            return txt.Substring(0, txt.Length - prefix.Length);
        }

        return txt;
    }

    public static string TrimStart(this string txt, string prefix)
    {
        if (txt.StartsWith(prefix))
        {
            return txt.Substring(prefix.Length);
        }

        return txt;
    }

    // Implements string.IsNullOrEmpty(...) as Extension Method.
    // This way it can be called as myString.IsNullOrEmpty(); instead string.IsNullOrEmpty(myString);
    public static bool IsNullOrEmpty(this string txt)
    {
        return string.IsNullOrEmpty(txt);
    }

    public static string NullToEmpty(this string txt)
    {
        if (txt == null)
        {
            return "";
        }

        return txt;
    }

    // Removes opening and ending part from a string.
    public static string Strip(this string txt, string opening, string ending)
    {
        if (txt == null)
        {
            return null;
        }

        if (txt.Length >= (opening.Length + ending.Length)
            && txt.StartsWith(opening)
            && txt.EndsWith(ending))
        {
            return txt.Substring(opening.Length, txt.Length - (opening.Length + ending.Length));
        }
        return txt;
    }

    public static bool StartsWith(this string txt, char c)
    {
        return txt != null && txt.Length > 0 && txt[0] == c;
    }

    public static bool EndsWith(this string txt, char c)
    {
        return txt != null && txt.Length > 0 && txt[txt.Length - 1] == c;
    }

    public static string ToLowerInvariantFirstChar(this string txt)
    {
        if (txt.Length <= 1)
        {
            return txt.ToLowerInvariant();
        }
        return txt.Substring(0, 1).ToLowerInvariant() + txt.Substring(1, txt.Length - 1);
    }

    public static string ToUpperInvariantFirstChar(this string txt)
    {
        if (txt.Length <= 1)
        {
            return txt.ToUpperInvariant();
        }
        return txt.Substring(0, 1).ToUpperInvariant() + txt.Substring(1, txt.Length - 1);
    }

    public static void AppendLine(this StringBuilder sb, string line, int indentationInSpaces)
    {
        for (int i = 0; i < indentationInSpaces; i++)
        {
            sb.Append(" ");
        }
        sb.Append(line);
        sb.Append("\n");
    }

    // https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
    public static string SplitCamelCase( this string str )
    {
        return Regex.Replace(
            Regex.Replace(
                str,
                @"(\P{Ll})(\P{Ll}\p{Ll})",
                "$1 $2"
            ),
            @"(\p{Ll})(\P{Ll})",
            "$1 $2"
        );
    }
}
