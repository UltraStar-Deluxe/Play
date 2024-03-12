using UnityEngine;

public static class ColorGenerationUtils
{
    private static readonly HashBasedColorGenerator colorGenerator = new(
        o => o?.GetHashCode() ?? 0,
        new Vector2(0.4f, 1f),
        new Vector2(0.7f, 1f));

    public static Color32 FromString(string text)
    {
        if (text.IsNullOrEmpty())
        {
            return Color.white;
        }

        return colorGenerator.ToColor(text);
    }
}
