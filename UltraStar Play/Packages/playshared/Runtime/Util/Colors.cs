using System;
using UnityEngine;
using Random = UnityEngine.Random;

// CSS colors as constants for Unity3D.
// List of CSS colors at W3Schools: https://www.w3schools.com/cssref/css_colors.asp
public static class Colors
{
    public static readonly Color32 clearBlack = CreateColor("#00000000");
    public static readonly Color32 clearWhite = CreateColor("#FFFFFF00");

    public static readonly Color32 aliceBlue = CreateColor("#F0F8FF");
    public static readonly Color32 antiqueWhite = CreateColor("#FAEBD7");
    public static readonly Color32 aqua = CreateColor("#00FFFF");
    public static readonly Color32 aquamarine = CreateColor("#7FFFD4");
    public static readonly Color32 azure = CreateColor("#F0FFFF");
    public static readonly Color32 beige = CreateColor("#F5F5DC");
    public static readonly Color32 bisque = CreateColor("#FFE4C4");
    public static readonly Color32 black = CreateColor("#000000");
    public static readonly Color32 blanchedAlmond = CreateColor("#FFEBCD");
    public static readonly Color32 blue = CreateColor("#0000FF");
    public static readonly Color32 blueViolet = CreateColor("#8A2BE2");
    public static readonly Color32 bronze = CreateColor("#8C7853");
    public static readonly Color32 brown = CreateColor("#A52A2A");
    public static readonly Color32 burlyWood = CreateColor("#DEB887");
    public static readonly Color32 cadetBlue = CreateColor("#5F9EA0");
    public static readonly Color32 chartreuse = CreateColor("#7FFF00");
    public static readonly Color32 chocolate = CreateColor("#D2691E");
    public static readonly Color32 coral = CreateColor("#FF7F50");
    public static readonly Color32 cornflowerBlue = CreateColor("#6495ED");
    public static readonly Color32 cornsilk = CreateColor("#FFF8DC");
    public static readonly Color32 crimson = CreateColor("#DC143C");
    public static readonly Color32 cyan = CreateColor("#00FFFF");
    public static readonly Color32 darkBlue = CreateColor("#00008B");
    public static readonly Color32 darkCyan = CreateColor("#008B8B");
    public static readonly Color32 darkGoldenRod = CreateColor("#B8860B");
    public static readonly Color32 darkGray = CreateColor("#A9A9A9");
    public static readonly Color32 darkGrey = CreateColor("#A9A9A9");
    public static readonly Color32 darkGreen = CreateColor("#006400");
    public static readonly Color32 darkKhaki = CreateColor("#BDB76B");
    public static readonly Color32 darkMagenta = CreateColor("#8B008B");
    public static readonly Color32 darkOliveGreen = CreateColor("#556B2F");
    public static readonly Color32 darkOrange = CreateColor("#FF8C00");
    public static readonly Color32 darkOrchid = CreateColor("#9932CC");
    public static readonly Color32 darkRed = CreateColor("#8B0000");
    public static readonly Color32 darkSalmon = CreateColor("#E9967A");
    public static readonly Color32 darkSeaGreen = CreateColor("#8FBC8F");
    public static readonly Color32 darkSlateBlue = CreateColor("#483D8B");
    public static readonly Color32 darkSlateGray = CreateColor("#2F4F4F");
    public static readonly Color32 darkSlateGrey = CreateColor("#2F4F4F");
    public static readonly Color32 darkTurquoise = CreateColor("#00CED1");
    public static readonly Color32 darkViolet = CreateColor("#9400D3");
    public static readonly Color32 deepPink = CreateColor("#FF1493");
    public static readonly Color32 deepSkyBlue = CreateColor("#00BFFF");
    public static readonly Color32 dimGray = CreateColor("#696969");
    public static readonly Color32 dimGrey = CreateColor("#696969");
    public static readonly Color32 dodgerBlue = CreateColor("#1E90FF");
    public static readonly Color32 fireBrick = CreateColor("#B22222");
    public static readonly Color32 floralWhite = CreateColor("#FFFAF0");
    public static readonly Color32 forestGreen = CreateColor("#228B22");
    public static readonly Color32 fuchsia = CreateColor("#FF00FF");
    public static readonly Color32 gainsboro = CreateColor("#DCDCDC");
    public static readonly Color32 ghostWhite = CreateColor("#F8F8FF");
    public static readonly Color32 gold = CreateColor("#FFD700");
    public static readonly Color32 goldenRod = CreateColor("#DAA520");
    public static readonly Color32 gray = CreateColor("#808080");
    public static readonly Color32 grey = CreateColor("#808080");
    public static readonly Color32 green = CreateColor("#008000");
    public static readonly Color32 greenYellow = CreateColor("#ADFF2F");
    public static readonly Color32 honeyDew = CreateColor("#F0FFF0");
    public static readonly Color32 hotPink = CreateColor("#FF69B4");
    public static readonly Color32 indianRed = CreateColor("#CD5C5C");
    public static readonly Color32 indigo = CreateColor("#4B0082");
    public static readonly Color32 ivory = CreateColor("#FFFFF0");
    public static readonly Color32 khaki = CreateColor("#F0E68C");
    public static readonly Color32 lavender = CreateColor("#E6E6FA");
    public static readonly Color32 lavenderBlush = CreateColor("#FFF0F5");
    public static readonly Color32 lawnGreen = CreateColor("#7CFC00");
    public static readonly Color32 lemonChiffon = CreateColor("#FFFACD");
    public static readonly Color32 lightBlue = CreateColor("#ADD8E6");
    public static readonly Color32 lightCoral = CreateColor("#F08080");
    public static readonly Color32 lightCyan = CreateColor("#E0FFFF");
    public static readonly Color32 lightGoldenRodYellow = CreateColor("#FAFAD2");
    public static readonly Color32 lightGray = CreateColor("#D3D3D3");
    public static readonly Color32 lightGrey = CreateColor("#D3D3D3");
    public static readonly Color32 lightGreen = CreateColor("#90EE90");
    public static readonly Color32 lightPink = CreateColor("#FFB6C1");
    public static readonly Color32 lightSalmon = CreateColor("#FFA07A");
    public static readonly Color32 lightSeaGreen = CreateColor("#20B2AA");
    public static readonly Color32 lightSkyBlue = CreateColor("#87CEFA");
    public static readonly Color32 lightSlateGray = CreateColor("#778899");
    public static readonly Color32 lightSlateGrey = CreateColor("#778899");
    public static readonly Color32 lightSteelBlue = CreateColor("#B0C4DE");
    public static readonly Color32 lightYellow = CreateColor("#FFFFE0");
    public static readonly Color32 lime = CreateColor("#00FF00");
    public static readonly Color32 limeGreen = CreateColor("#32CD32");
    public static readonly Color32 linen = CreateColor("#FAF0E6");
    public static readonly Color32 magenta = CreateColor("#FF00FF");
    public static readonly Color32 maroon = CreateColor("#800000");
    public static readonly Color32 mediumAquaMarine = CreateColor("#66CDAA");
    public static readonly Color32 mediumBlue = CreateColor("#0000CD");
    public static readonly Color32 mediumOrchid = CreateColor("#BA55D3");
    public static readonly Color32 mediumPurple = CreateColor("#9370DB");
    public static readonly Color32 mediumSeaGreen = CreateColor("#3CB371");
    public static readonly Color32 mediumSlateBlue = CreateColor("#7B68EE");
    public static readonly Color32 mediumSpringGreen = CreateColor("#00FA9A");
    public static readonly Color32 mediumTurquoise = CreateColor("#48D1CC");
    public static readonly Color32 mediumVioletRed = CreateColor("#C71585");
    public static readonly Color32 midnightBlue = CreateColor("#191970");
    public static readonly Color32 mintCream = CreateColor("#F5FFFA");
    public static readonly Color32 mistyRose = CreateColor("#FFE4E1");
    public static readonly Color32 moccasin = CreateColor("#FFE4B5");
    public static readonly Color32 navajoWhite = CreateColor("#FFDEAD");
    public static readonly Color32 navy = CreateColor("#000080");
    public static readonly Color32 oldLace = CreateColor("#FDF5E6");
    public static readonly Color32 olive = CreateColor("#808000");
    public static readonly Color32 oliveDrab = CreateColor("#6B8E23");
    public static readonly Color32 orange = CreateColor("#FFA500");
    public static readonly Color32 orangeRed = CreateColor("#FF4500");
    public static readonly Color32 orchid = CreateColor("#DA70D6");
    public static readonly Color32 paleGoldenRod = CreateColor("#EEE8AA");
    public static readonly Color32 paleGreen = CreateColor("#98FB98");
    public static readonly Color32 paleTurquoise = CreateColor("#AFEEEE");
    public static readonly Color32 paleVioletRed = CreateColor("#DB7093");
    public static readonly Color32 papayaWhip = CreateColor("#FFEFD5");
    public static readonly Color32 peachPuff = CreateColor("#FFDAB9");
    public static readonly Color32 peru = CreateColor("#CD853F");
    public static readonly Color32 pink = CreateColor("#FFC0CB");
    public static readonly Color32 plum = CreateColor("#DDA0DD");
    public static readonly Color32 powderBlue = CreateColor("#B0E0E6");
    public static readonly Color32 purple = CreateColor("#800080");
    public static readonly Color32 rebeccaPurple = CreateColor("#663399");
    public static readonly Color32 red = CreateColor("#FF0000");
    public static readonly Color32 rosyBrown = CreateColor("#BC8F8F");
    public static readonly Color32 royalBlue = CreateColor("#4169E1");
    public static readonly Color32 saddleBrown = CreateColor("#8B4513");
    public static readonly Color32 salmon = CreateColor("#FA8072");
    public static readonly Color32 sandyBrown = CreateColor("#F4A460");
    public static readonly Color32 seaGreen = CreateColor("#2E8B57");
    public static readonly Color32 seaShell = CreateColor("#FFF5EE");
    public static readonly Color32 sienna = CreateColor("#A0522D");
    public static readonly Color32 silver = CreateColor("#C0C0C0");
    public static readonly Color32 skyBlue = CreateColor("#87CEEB");
    public static readonly Color32 slateBlue = CreateColor("#6A5ACD");
    public static readonly Color32 slateGray = CreateColor("#708090");
    public static readonly Color32 slateGrey = CreateColor("#708090");
    public static readonly Color32 snow = CreateColor("#FFFAFA");
    public static readonly Color32 springGreen = CreateColor("#00FF7F");
    public static readonly Color32 steelBlue = CreateColor("#4682B4");
    public static readonly Color32 tan = CreateColor("#D2B48C");
    public static readonly Color32 teal = CreateColor("#008080");
    public static readonly Color32 thistle = CreateColor("#D8BFD8");
    public static readonly Color32 tomato = CreateColor("#FF6347");
    public static readonly Color32 turquoise = CreateColor("#40E0D0");
    public static readonly Color32 violet = CreateColor("#EE82EE");
    public static readonly Color32 wheat = CreateColor("#F5DEB3");
    public static readonly Color32 white = CreateColor("#FFFFFF");
    public static readonly Color32 whiteSmoke = CreateColor("#F5F5F5");
    public static readonly Color32 yellow = CreateColor("#FFFF00");
    public static readonly Color32 yellowGreen = CreateColor("#9ACD32");

    public static string ToHexColor(Color32 color)
    {
        return color.a == 255
            ? ColorUtility.ToHtmlStringRGB(color)
            : ColorUtility.ToHtmlStringRGBA(color);
    }

    public static bool TryParseHexColor(string hexColor, out Color32 color, byte alpha = 255)
    {
        try
        {
            color = CreateColor(hexColor, alpha);
            return true;
        }
        catch (Exception ex)
        {
            color = white;
            return false;
        }
    }

    public static Color32 CreateRandomColor(byte alpha = 255)
    {
        byte r = (byte)Random.Range(0, 255);
        byte g = (byte)Random.Range(0, 255);
        byte b = (byte)Random.Range(0, 255);
        return new Color32(r, g, b, alpha);
    }
    
    /**
     * ColorUtility.TryParseHtmlString cannot be called during serialization.
     * But this function can.
     */
    public static Color32 CreateColor(string hexColor, byte alpha = 255)
    {
        string originalHexColor = hexColor;
        if (hexColor.StartsWith("#"))
        {
            hexColor = hexColor.Substring(1);
        }

        try
        {
            string hexR = hexColor.Substring(0, 2);
            string hexG = hexColor.Substring(2, 2);
            string hexB = hexColor.Substring(4, 2);

            byte intR = byte.Parse(hexR, System.Globalization.NumberStyles.HexNumber);
            byte intG = byte.Parse(hexG, System.Globalization.NumberStyles.HexNumber);
            byte intB = byte.Parse(hexB, System.Globalization.NumberStyles.HexNumber);

            byte intA;
            if (hexColor.Length > 6)
            {
                string hexA = hexColor.Substring(6, 2);
                intA = byte.Parse(hexA, System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                intA = alpha;
            }

            return new Color32(intR, intG, intB, intA);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Cannot create Color32 for {originalHexColor}", e);
        }
    }
    
    public static Color HsvOffset(Color inputColor, float hueOffset, float saturationOffset, float valueOffset)
    {
        float h, s, v;
        Color.RGBToHSV(inputColor, out h, out s, out v);
        h += hueOffset;
        s += saturationOffset;
        v += valueOffset;
        return Color.HSVToRGB(h, s, v);
    }
}
