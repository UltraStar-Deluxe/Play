
using System;
using UnityEngine;

/**
 * Generates a color from an object by using a hash function.
 */
public class HashBasedColorGenerator
{
    public static readonly HashBasedColorGenerator defaultInstance = new HashBasedColorGenerator(
        o => o?.GetHashCode() ?? 0,
        new Vector2(0, 1),
        new Vector2(0, 1));

    private static readonly int first10BitsSet = (int)(Math.Pow(2, 10) - 1);
    private static readonly int first12BitsSet = (int)(Math.Pow(2, 12) - 1);

    private readonly Func<object, int> hashFunction;
    private readonly Vector2 saturationRange;
    private readonly Vector2 valueRange;

    public HashBasedColorGenerator(Func<object, int> hashFunction, Vector2 saturationRange, Vector2 valueRange)
    {
        this.hashFunction = hashFunction;
        this.saturationRange = saturationRange;
        this.valueRange = valueRange;
    }

    public Color32 ToColor(object o)
    {
        int hash = Math.Abs(this.hashFunction(o));
        
        // The first 12 bits of the hash are for the hue,
        // the second 10 bits for saturation,
        // the third 10 bits for value.
        int hueBitsOfHash = hash & first12BitsSet;
        int saturationBitsOfHash = (hash >> 12) & first10BitsSet;
        int valueBitsOfHash = (hash >> 22) & first10BitsSet;
        
        double hue0To1 = (double)hueBitsOfHash / first12BitsSet;
        double saturation0To1 = saturationRange.x + ((double)saturationBitsOfHash / first10BitsSet) * (saturationRange.y - saturationRange.x);
        double value0To1 = valueRange.x + ((double)valueBitsOfHash / first10BitsSet) * (valueRange.y - valueRange.x);

        byte hue = (byte)(hue0To1 * 255.0);
        byte saturation = (byte)(saturation0To1 * 255.0);
        byte value = (byte)(value0To1 * 255.0);
        byte alpha = 255;

        Color32 hsvColor = new Color32(hue, saturation, value, alpha);
        Color32 rgbColor = hsvColor.HsvToRgb();
        return rgbColor;
    }
}
