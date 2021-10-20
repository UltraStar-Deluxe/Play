using UnityEngine;

public static class RectTransformUtils
{
    public static bool IsMouseOverRectTransform(RectTransform rectTransform)
    {
        Vector2 localMousePosition = rectTransform.InverseTransformPoint(Input.mousePosition);
        return rectTransform.rect.Contains(localMousePosition);
    }

    public static Rect GetScreenCoordinates(RectTransform rectTransform)
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);
        Rect result = new Rect(
            worldCorners[0].x,
            worldCorners[0].y,
            worldCorners[2].x - worldCorners[0].x,
            worldCorners[2].y - worldCorners[0].y);
        return result;
    }
}
