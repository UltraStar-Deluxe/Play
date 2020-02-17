using UnityEngine;
using UnityEngine.UI;

public static class ImageExtensions
{
    public static void SetAlpha(this Image image, float newAlpha)
    {
        Color lastColor = image.color;
        image.color = new Color(lastColor.r, lastColor.g, lastColor.b, newAlpha);
    }

    // Make the color of an image darker with a factor < 1, or brighter with a factor > 1.
    public static void MultiplyColor(this Image image, float factor, bool includeAlpha = false)
    {
        Color lastColor = image.color;
        float newR = NumberUtils.Limit(lastColor.r * factor, 0, 1);
        float newG = NumberUtils.Limit(lastColor.g * factor, 0, 1);
        float newB = NumberUtils.Limit(lastColor.b * factor, 0, 1);
        float newAlpha = includeAlpha ? NumberUtils.Limit(lastColor.a * factor, 0, 1) : lastColor.a;
        image.color = new Color(newR, newG, newB, newAlpha);
    }
}