using System;
using UnityEngine;

// CSS colors as constants for Unity3D.
// List of CSS colors at W3Schools: https://www.w3schools.com/cssref/css_colors.asp
public static class Colors
{
    public static Color AliceBlue = CreateColor("#F0F8FF");
    public static Color AntiqueWhite = CreateColor("#FAEBD7");
    public static Color Aqua = CreateColor("#00FFFF");
    public static Color Aquamarine = CreateColor("#7FFFD4");
    public static Color Azure = CreateColor("#F0FFFF");
    public static Color Beige = CreateColor("#F5F5DC");
    public static Color Bisque = CreateColor("#FFE4C4");
    public static Color Black = CreateColor("#000000");
    public static Color BlanchedAlmond = CreateColor("#FFEBCD");
    public static Color Blue = CreateColor("#0000FF");
    public static Color BlueViolet = CreateColor("#8A2BE2");
    public static Color Brown = CreateColor("#A52A2A");
    public static Color BurlyWood = CreateColor("#DEB887");
    public static Color CadetBlue = CreateColor("#5F9EA0");
    public static Color Chartreuse = CreateColor("#7FFF00");
    public static Color Chocolate = CreateColor("#D2691E");
    public static Color Coral = CreateColor("#FF7F50");
    public static Color CornflowerBlue = CreateColor("#6495ED");
    public static Color Cornsilk = CreateColor("#FFF8DC");
    public static Color Crimson = CreateColor("#DC143C");
    public static Color Cyan = CreateColor("#00FFFF");
    public static Color DarkBlue = CreateColor("#00008B");
    public static Color DarkCyan = CreateColor("#008B8B");
    public static Color DarkGoldenRod = CreateColor("#B8860B");
    public static Color DarkGray = CreateColor("#A9A9A9");
    public static Color DarkGrey = CreateColor("#A9A9A9");
    public static Color DarkGreen = CreateColor("#006400");
    public static Color DarkKhaki = CreateColor("#BDB76B");
    public static Color DarkMagenta = CreateColor("#8B008B");
    public static Color DarkOliveGreen = CreateColor("#556B2F");
    public static Color DarkOrange = CreateColor("#FF8C00");
    public static Color DarkOrchid = CreateColor("#9932CC");
    public static Color DarkRed = CreateColor("#8B0000");
    public static Color DarkSalmon = CreateColor("#E9967A");
    public static Color DarkSeaGreen = CreateColor("#8FBC8F");
    public static Color DarkSlateBlue = CreateColor("#483D8B");
    public static Color DarkSlateGray = CreateColor("#2F4F4F");
    public static Color DarkSlateGrey = CreateColor("#2F4F4F");
    public static Color DarkTurquoise = CreateColor("#00CED1");
    public static Color DarkViolet = CreateColor("#9400D3");
    public static Color DeepPink = CreateColor("#FF1493");
    public static Color DeepSkyBlue = CreateColor("#00BFFF");
    public static Color DimGray = CreateColor("#696969");
    public static Color DimGrey = CreateColor("#696969");
    public static Color DodgerBlue = CreateColor("#1E90FF");
    public static Color FireBrick = CreateColor("#B22222");
    public static Color FloralWhite = CreateColor("#FFFAF0");
    public static Color ForestGreen = CreateColor("#228B22");
    public static Color Fuchsia = CreateColor("#FF00FF");
    public static Color Gainsboro = CreateColor("#DCDCDC");
    public static Color GhostWhite = CreateColor("#F8F8FF");
    public static Color Gold = CreateColor("#FFD700");
    public static Color GoldenRod = CreateColor("#DAA520");
    public static Color Gray = CreateColor("#808080");
    public static Color Grey = CreateColor("#808080");
    public static Color Green = CreateColor("#008000");
    public static Color GreenYellow = CreateColor("#ADFF2F");
    public static Color HoneyDew = CreateColor("#F0FFF0");
    public static Color HotPink = CreateColor("#FF69B4");
    public static Color IndianRed = CreateColor("#CD5C5C");
    public static Color Indigo = CreateColor("#4B0082");
    public static Color Ivory = CreateColor("#FFFFF0");
    public static Color Khaki = CreateColor("#F0E68C");
    public static Color Lavender = CreateColor("#E6E6FA");
    public static Color LavenderBlush = CreateColor("#FFF0F5");
    public static Color LawnGreen = CreateColor("#7CFC00");
    public static Color LemonChiffon = CreateColor("#FFFACD");
    public static Color LightBlue = CreateColor("#ADD8E6");
    public static Color LightCoral = CreateColor("#F08080");
    public static Color LightCyan = CreateColor("#E0FFFF");
    public static Color LightGoldenRodYellow = CreateColor("#FAFAD2");
    public static Color LightGray = CreateColor("#D3D3D3");
    public static Color LightGrey = CreateColor("#D3D3D3");
    public static Color LightGreen = CreateColor("#90EE90");
    public static Color LightPink = CreateColor("#FFB6C1");
    public static Color LightSalmon = CreateColor("#FFA07A");
    public static Color LightSeaGreen = CreateColor("#20B2AA");
    public static Color LightSkyBlue = CreateColor("#87CEFA");
    public static Color LightSlateGray = CreateColor("#778899");
    public static Color LightSlateGrey = CreateColor("#778899");
    public static Color LightSteelBlue = CreateColor("#B0C4DE");
    public static Color LightYellow = CreateColor("#FFFFE0");
    public static Color Lime = CreateColor("#00FF00");
    public static Color LimeGreen = CreateColor("#32CD32");
    public static Color Linen = CreateColor("#FAF0E6");
    public static Color Magenta = CreateColor("#FF00FF");
    public static Color Maroon = CreateColor("#800000");
    public static Color MediumAquaMarine = CreateColor("#66CDAA");
    public static Color MediumBlue = CreateColor("#0000CD");
    public static Color MediumOrchid = CreateColor("#BA55D3");
    public static Color MediumPurple = CreateColor("#9370DB");
    public static Color MediumSeaGreen = CreateColor("#3CB371");
    public static Color MediumSlateBlue = CreateColor("#7B68EE");
    public static Color MediumSpringGreen = CreateColor("#00FA9A");
    public static Color MediumTurquoise = CreateColor("#48D1CC");
    public static Color MediumVioletRed = CreateColor("#C71585");
    public static Color MidnightBlue = CreateColor("#191970");
    public static Color MintCream = CreateColor("#F5FFFA");
    public static Color MistyRose = CreateColor("#FFE4E1");
    public static Color Moccasin = CreateColor("#FFE4B5");
    public static Color NavajoWhite = CreateColor("#FFDEAD");
    public static Color Navy = CreateColor("#000080");
    public static Color OldLace = CreateColor("#FDF5E6");
    public static Color Olive = CreateColor("#808000");
    public static Color OliveDrab = CreateColor("#6B8E23");
    public static Color Orange = CreateColor("#FFA500");
    public static Color OrangeRed = CreateColor("#FF4500");
    public static Color Orchid = CreateColor("#DA70D6");
    public static Color PaleGoldenRod = CreateColor("#EEE8AA");
    public static Color PaleGreen = CreateColor("#98FB98");
    public static Color PaleTurquoise = CreateColor("#AFEEEE");
    public static Color PaleVioletRed = CreateColor("#DB7093");
    public static Color PapayaWhip = CreateColor("#FFEFD5");
    public static Color PeachPuff = CreateColor("#FFDAB9");
    public static Color Peru = CreateColor("#CD853F");
    public static Color Pink = CreateColor("#FFC0CB");
    public static Color Plum = CreateColor("#DDA0DD");
    public static Color PowderBlue = CreateColor("#B0E0E6");
    public static Color Purple = CreateColor("#800080");
    public static Color RebeccaPurple = CreateColor("#663399");
    public static Color Red = CreateColor("#FF0000");
    public static Color RosyBrown = CreateColor("#BC8F8F");
    public static Color RoyalBlue = CreateColor("#4169E1");
    public static Color SaddleBrown = CreateColor("#8B4513");
    public static Color Salmon = CreateColor("#FA8072");
    public static Color SandyBrown = CreateColor("#F4A460");
    public static Color SeaGreen = CreateColor("#2E8B57");
    public static Color SeaShell = CreateColor("#FFF5EE");
    public static Color Sienna = CreateColor("#A0522D");
    public static Color Silver = CreateColor("#C0C0C0");
    public static Color SkyBlue = CreateColor("#87CEEB");
    public static Color SlateBlue = CreateColor("#6A5ACD");
    public static Color SlateGray = CreateColor("#708090");
    public static Color SlateGrey = CreateColor("#708090");
    public static Color Snow = CreateColor("#FFFAFA");
    public static Color SpringGreen = CreateColor("#00FF7F");
    public static Color SteelBlue = CreateColor("#4682B4");
    public static Color Tan = CreateColor("#D2B48C");
    public static Color Teal = CreateColor("#008080");
    public static Color Thistle = CreateColor("#D8BFD8");
    public static Color Tomato = CreateColor("#FF6347");
    public static Color Turquoise = CreateColor("#40E0D0");
    public static Color Violet = CreateColor("#EE82EE");
    public static Color Wheat = CreateColor("#F5DEB3");
    public static Color White = CreateColor("#FFFFFF");
    public static Color WhiteSmoke = CreateColor("#F5F5F5");
    public static Color Yellow = CreateColor("#FFFF00");
    public static Color YellowGreen = CreateColor("#9ACD32");

    public static Color CreateColor(string hexColor)
    {
        // ColorUtility.TryParseHtmlString cannot be called during serialization.
        // But this function can...
        try
        {
            string hexR = hexColor.Substring(1, 2);
            string hexG = hexColor.Substring(3, 2);
            string hexB = hexColor.Substring(5, 2);

            int intR = int.Parse(hexR, System.Globalization.NumberStyles.HexNumber);
            int intG = int.Parse(hexG, System.Globalization.NumberStyles.HexNumber);
            int intB = int.Parse(hexB, System.Globalization.NumberStyles.HexNumber);

            float floatR = (float)intR / 255.0f;
            float floatG = (float)intG / 255.0f;
            float floatB = (float)intB / 255.0f;

            return new Color(floatR, floatG, floatB);
        }
        catch (Exception e)
        {
            Debug.Log($"Cannot create Color for {hexColor}: " + e.ToString());
        }
        return Color.white;
    }
}