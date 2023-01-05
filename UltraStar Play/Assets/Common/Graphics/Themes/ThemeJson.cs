using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

/**
 * Data structure for theme files.
 * The fields correspond to the possible properties defined in a JSON theme file.
 */
[Serializable]
public class ThemeJson
{
    public DynamicBackgroundJson dynamicBackground;
    public SongRatingIconsJson songRatingIcons;
    public Color32 buttonMainColor;
    public Color32 fontColorButtons;
    public Color32 fontColorLabels;
    public Color32 fontColor;

    public static ThemeJson LoadFromJson(string json)
    {
        json = PreprocessJson(json);
        ThemeJson theme = JsonUtility.FromJson<ThemeJson>(json);
        if (theme == null)
        {
            throw new Exception("Couldn't parse supplied JSON as ThemeSettings data.");
        }
        return theme;
    }

    // Process JSON to parse certain values, e.g. allow hex colors and convert
    // them to proper color struct notation
    static string PreprocessJson(string input)
    {
        // Convert hex colors to proper JSON color struct
        Regex hexRgbaDouble = new Regex(@"""#([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])""");
        Regex hexRgbDouble = new Regex(@"""#([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])""");
        Regex hexRgba = new Regex(@"""#([0-9A-Fa-f])([0-9A-Fa-f])([0-9A-Fa-f])([0-9A-Fa-f])""");
        Regex hexRgb = new Regex(@"""#([0-9A-Fa-f])([0-9A-Fa-f])([0-9A-Fa-f])""");

        int offset = 0;
        hexRgbaDouble.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToInt(match.Groups[1].Value)}, \"g\":{HexToInt(match.Groups[2].Value)}, \"b\":{HexToInt(match.Groups[3].Value)}, \"a\":{HexToInt(match.Groups[4].Value)} }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });
        offset = 0;
        hexRgbDouble.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToInt(match.Groups[1].Value)}, \"g\":{HexToInt(match.Groups[2].Value)}, \"b\":{HexToInt(match.Groups[3].Value)}, \"a\":255 }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });
        offset = 0;
        hexRgba.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToInt(match.Groups[1].Value, true)}, \"g\":{HexToInt(match.Groups[2].Value, true)}, \"b\":{HexToInt(match.Groups[3].Value, true)}, \"a\":{HexToInt(match.Groups[4].Value, true)} }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });
        offset = 0;
        hexRgb.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToInt(match.Groups[1].Value, true)}, \"g\":{HexToInt(match.Groups[2].Value, true)}, \"b\":{HexToInt(match.Groups[3].Value, true)}, \"a\":255 }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });

        return input;
    }

    static int HexToInt(string hex, bool singleDigit = false)
    {
        return Convert.ToInt32(singleDigit ? $"{hex}{hex}" : hex, 16);
    }
}
