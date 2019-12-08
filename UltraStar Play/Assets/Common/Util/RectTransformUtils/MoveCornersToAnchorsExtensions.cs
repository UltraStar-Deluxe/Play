using UnityEngine;

public static class MoveCornersToAnchorsExtensions
{
    public static void MoveCornersToAnchors(this RectTransform rectTransform)
    {
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public static void MoveCornersToAnchors_Width(this RectTransform rectTransform)
    {
        rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
    }

    public static void MoveCornersToAnchors_Height(this RectTransform rectTransform)
    {
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 0);
    }

    public static void MoveCornersToAnchors_CenterPosition(this RectTransform rectTransform)
    {
        rectTransform.anchoredPosition = Vector2.zero;
    }
}