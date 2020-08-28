using UnityEngine;

public static class RectTransformUtils
{
    public static bool IsMouseOverRectTransform(RectTransform rectTransform)
    {
        Vector2 localMousePosition = rectTransform.InverseTransformPoint(Input.mousePosition);
        return rectTransform.rect.Contains(localMousePosition);
    }
}
