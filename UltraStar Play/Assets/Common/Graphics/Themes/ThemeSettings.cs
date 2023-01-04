using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class ThemeSettings
{
    // These correspond to the possible JSON properties defined in a theme file.
    // Eventually a theme builder UI can be considered, but meanwhile this will
    // have to be written manually with a text editor.
    // See the files in the "themes" folder at the root of the project/build.

    public DynamicBackground dynamicBackground;
    public SongRatingIcons songRatingIcons;
    public Color buttonMainColor;
    public Color fontColorButtons;
    public Color fontColorLabels;
    public Color fontColor;

    public static ThemeSettings LoadFromJson(string json)
    {
        json = PreprocessJson(json);
        ThemeSettings theme = JsonUtility.FromJson<ThemeSettings>(json);
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
            string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value)}, \"g\":{HexToFloatStr(match.Groups[2].Value)}, \"b\":{HexToFloatStr(match.Groups[3].Value)}, \"a\":{HexToFloatStr(match.Groups[4].Value)} }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });
        offset = 0;
        hexRgbDouble.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value)}, \"g\":{HexToFloatStr(match.Groups[2].Value)}, \"b\":{HexToFloatStr(match.Groups[3].Value)}, \"a\":1.0 }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });
        offset = 0;
        hexRgba.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value, true)}, \"g\":{HexToFloatStr(match.Groups[2].Value, true)}, \"b\":{HexToFloatStr(match.Groups[3].Value, true)}, \"a\":{HexToFloatStr(match.Groups[4].Value, true)} }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });
        offset = 0;
        hexRgb.Matches(input).ForEach(match =>
        {
            string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value, true)}, \"g\":{HexToFloatStr(match.Groups[2].Value, true)}, \"b\":{HexToFloatStr(match.Groups[3].Value, true)}, \"a\":1.0 }}";
            input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        });

        return input;
    }

    static string HexToFloatStr(string hex, bool singleDigit = false)
    {
        return (Convert.ToInt32(singleDigit ? $"{hex}{hex}" : hex, 16)/255.0f).ToString(CultureInfo.InvariantCulture);
    }
}
