using System;
using UnityEngine;

// CSS colors as constants for Unity3D.
// List of CSS colors at W3Schools: https://www.w3schools.com/cssref/css_colors.asp
public static class Colors
{
    public static readonly Color aliceBlue = CreateColor("#F0F8FF");
    public static readonly Color antiqueWhite = CreateColor("#FAEBD7");
    public static readonly Color aqua = CreateColor("#00FFFF");
    public static readonly Color aquamarine = CreateColor("#7FFFD4");
    public static readonly Color azure = CreateColor("#F0FFFF");
    public static readonly Color beige = CreateColor("#F5F5DC");
    public static readonly Color bisque = CreateColor("#FFE4C4");
    public static readonly Color black = CreateColor("#000000");
    public static readonly Color blanchedAlmond = CreateColor("#FFEBCD");
    public static readonly Color blue = CreateColor("#0000FF");
    public static readonly Color blueViolet = CreateColor("#8A2BE2");
    public static readonly Color brown = CreateColor("#A52A2A");
    public static readonly Color burlyWood = CreateColor("#DEB887");
    public static readonly Color cadetBlue = CreateColor("#5F9EA0");
    public static readonly Color chartreuse = CreateColor("#7FFF00");
    public static readonly Color chocolate = CreateColor("#D2691E");
    public static readonly Color coral = CreateColor("#FF7F50");
    public static readonly Color cornflowerBlue = CreateColor("#6495ED");
    public static readonly Color cornsilk = CreateColor("#FFF8DC");
    public static readonly Color crimson = CreateColor("#DC143C");
    public static readonly Color cyan = CreateColor("#00FFFF");
    public static readonly Color darkBlue = CreateColor("#00008B");
    public static readonly Color darkCyan = CreateColor("#008B8B");
    public static readonly Color darkGoldenRod = CreateColor("#B8860B");
    public static readonly Color darkGray = CreateColor("#A9A9A9");
    public static readonly Color darkGrey = CreateColor("#A9A9A9");
    public static readonly Color darkGreen = CreateColor("#006400");
    public static readonly Color darkKhaki = CreateColor("#BDB76B");
    public static readonly Color darkMagenta = CreateColor("#8B008B");
    public static readonly Color darkOliveGreen = CreateColor("#556B2F");
    public static readonly Color darkOrange = CreateColor("#FF8C00");
    public static readonly Color darkOrchid = CreateColor("#9932CC");
    public static readonly Color darkRed = CreateColor("#8B0000");
    public static readonly Color darkSalmon = CreateColor("#E9967A");
    public static readonly Color darkSeaGreen = CreateColor("#8FBC8F");
    public static readonly Color darkSlateBlue = CreateColor("#483D8B");
    public static readonly Color darkSlateGray = CreateColor("#2F4F4F");
    public static readonly Color darkSlateGrey = CreateColor("#2F4F4F");
    public static readonly Color darkTurquoise = CreateColor("#00CED1");
    public static readonly Color darkViolet = CreateColor("#9400D3");
    public static readonly Color deepPink = CreateColor("#FF1493");
    public static readonly Color deepSkyBlue = CreateColor("#00BFFF");
    public static readonly Color dimGray = CreateColor("#696969");
    public static readonly Color dimGrey = CreateColor("#696969");
    public static readonly Color dodgerBlue = CreateColor("#1E90FF");
    public static readonly Color fireBrick = CreateColor("#B22222");
    public static readonly Color floralWhite = CreateColor("#FFFAF0");
    public static readonly Color forestGreen = CreateColor("#228B22");
    public static readonly Color fuchsia = CreateColor("#FF00FF");
    public static readonly Color gainsboro = CreateColor("#DCDCDC");
    public static readonly Color ghostWhite = CreateColor("#F8F8FF");
    public static readonly Color gold = CreateColor("#FFD700");
    public static readonly Color goldenRod = CreateColor("#DAA520");
    public static readonly Color gray = CreateColor("#808080");
    public static readonly Color grey = CreateColor("#808080");
    public static readonly Color green = CreateColor("#008000");
    public static readonly Color greenYellow = CreateColor("#ADFF2F");
    public static readonly Color honeyDew = CreateColor("#F0FFF0");
    public static readonly Color hotPink = CreateColor("#FF69B4");
    public static readonly Color indianRed = CreateColor("#CD5C5C");
    public static readonly Color indigo = CreateColor("#4B0082");
    public static readonly Color ivory = CreateColor("#FFFFF0");
    public static readonly Color khaki = CreateColor("#F0E68C");
    public static readonly Color lavender = CreateColor("#E6E6FA");
    public static readonly Color lavenderBlush = CreateColor("#FFF0F5");
    public static readonly Color lawnGreen = CreateColor("#7CFC00");
    public static readonly Color lemonChiffon = CreateColor("#FFFACD");
    public static readonly Color lightBlue = CreateColor("#ADD8E6");
    public static readonly Color lightCoral = CreateColor("#F08080");
    public static readonly Color lightCyan = CreateColor("#E0FFFF");
    public static readonly Color lightGoldenRodYellow = CreateColor("#FAFAD2");
    public static readonly Color lightGray = CreateColor("#D3D3D3");
    public static readonly Color lightGrey = CreateColor("#D3D3D3");
    public static readonly Color lightGreen = CreateColor("#90EE90");
    public static readonly Color lightPink = CreateColor("#FFB6C1");
    public static readonly Color lightSalmon = CreateColor("#FFA07A");
    public static readonly Color lightSeaGreen = CreateColor("#20B2AA");
    public static readonly Color lightSkyBlue = CreateColor("#87CEFA");
    public static readonly Color lightSlateGray = CreateColor("#778899");
    public static readonly Color lightSlateGrey = CreateColor("#778899");
    public static readonly Color lightSteelBlue = CreateColor("#B0C4DE");
    public static readonly Color lightYellow = CreateColor("#FFFFE0");
    public static readonly Color lime = CreateColor("#00FF00");
    public static readonly Color limeGreen = CreateColor("#32CD32");
    public static readonly Color linen = CreateColor("#FAF0E6");
    public static readonly Color magenta = CreateColor("#FF00FF");
    public static readonly Color maroon = CreateColor("#800000");
    public static readonly Color mediumAquaMarine = CreateColor("#66CDAA");
    public static readonly Color mediumBlue = CreateColor("#0000CD");
    public static readonly Color mediumOrchid = CreateColor("#BA55D3");
    public static readonly Color mediumPurple = CreateColor("#9370DB");
    public static readonly Color mediumSeaGreen = CreateColor("#3CB371");
    public static readonly Color mediumSlateBlue = CreateColor("#7B68EE");
    public static readonly Color mediumSpringGreen = CreateColor("#00FA9A");
    public static readonly Color mediumTurquoise = CreateColor("#48D1CC");
    public static readonly Color mediumVioletRed = CreateColor("#C71585");
    public static readonly Color midnightBlue = CreateColor("#191970");
    public static readonly Color mintCream = CreateColor("#F5FFFA");
    public static readonly Color mistyRose = CreateColor("#FFE4E1");
    public static readonly Color moccasin = CreateColor("#FFE4B5");
    public static readonly Color navajoWhite = CreateColor("#FFDEAD");
    public static readonly Color navy = CreateColor("#000080");
    public static readonly Color oldLace = CreateColor("#FDF5E6");
    public static readonly Color olive = CreateColor("#808000");
    public static readonly Color oliveDrab = CreateColor("#6B8E23");
    public static readonly Color orange = CreateColor("#FFA500");
    public static readonly Color orangeRed = CreateColor("#FF4500");
    public static readonly Color orchid = CreateColor("#DA70D6");
    public static readonly Color paleGoldenRod = CreateColor("#EEE8AA");
    public static readonly Color paleGreen = CreateColor("#98FB98");
    public static readonly Color paleTurquoise = CreateColor("#AFEEEE");
    public static readonly Color paleVioletRed = CreateColor("#DB7093");
    public static readonly Color papayaWhip = CreateColor("#FFEFD5");
    public static readonly Color peachPuff = CreateColor("#FFDAB9");
    public static readonly Color peru = CreateColor("#CD853F");
    public static readonly Color pink = CreateColor("#FFC0CB");
    public static readonly Color plum = CreateColor("#DDA0DD");
    public static readonly Color powderBlue = CreateColor("#B0E0E6");
    public static readonly Color purple = CreateColor("#800080");
    public static readonly Color rebeccaPurple = CreateColor("#663399");
    public static readonly Color red = CreateColor("#FF0000");
    public static readonly Color rosyBrown = CreateColor("#BC8F8F");
    public static readonly Color royalBlue = CreateColor("#4169E1");
    public static readonly Color saddleBrown = CreateColor("#8B4513");
    public static readonly Color salmon = CreateColor("#FA8072");
    public static readonly Color sandyBrown = CreateColor("#F4A460");
    public static readonly Color seaGreen = CreateColor("#2E8B57");
    public static readonly Color seaShell = CreateColor("#FFF5EE");
    public static readonly Color sienna = CreateColor("#A0522D");
    public static readonly Color silver = CreateColor("#C0C0C0");
    public static readonly Color skyBlue = CreateColor("#87CEEB");
    public static readonly Color slateBlue = CreateColor("#6A5ACD");
    public static readonly Color slateGray = CreateColor("#708090");
    public static readonly Color slateGrey = CreateColor("#708090");
    public static readonly Color snow = CreateColor("#FFFAFA");
    public static readonly Color springGreen = CreateColor("#00FF7F");
    public static readonly Color steelBlue = CreateColor("#4682B4");
    public static readonly Color tan = CreateColor("#D2B48C");
    public static readonly Color teal = CreateColor("#008080");
    public static readonly Color thistle = CreateColor("#D8BFD8");
    public static readonly Color tomato = CreateColor("#FF6347");
    public static readonly Color turquoise = CreateColor("#40E0D0");
    public static readonly Color violet = CreateColor("#EE82EE");
    public static readonly Color wheat = CreateColor("#F5DEB3");
    public static readonly Color white = CreateColor("#FFFFFF");
    public static readonly Color whiteSmoke = CreateColor("#F5F5F5");
    public static readonly Color yellow = CreateColor("#FFFF00");
    public static readonly Color yellowGreen = CreateColor("#9ACD32");

    public static Color CreateColor(string hexColor, float alpha = 1f)
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

            return new Color(floatR, floatG, floatB, alpha);
        }
        catch (Exception e)
        {
            Debug.Log($"Cannot create Color for {hexColor}: " + e);
        }
        return Color.white;
    }
}