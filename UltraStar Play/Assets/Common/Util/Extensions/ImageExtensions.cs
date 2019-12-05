using UnityEngine;
using UnityEngine.UI;

public static class ImageExtensions
{
    public static void SetAlpha(this Image image, float newAlpha)
    {
        Color lastColor = image.color;
        image.color = new Color(lastColor.r, lastColor.g, lastColor.b, newAlpha);
    }
}