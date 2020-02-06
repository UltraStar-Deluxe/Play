using UnityEngine;

public static class CanvasUtils
{
    public static Canvas FindCanvas()
    {
        Canvas canvas = GameObjectUtils.FindComponentWithTag<Canvas>("Canvas");
        if (canvas == null)
        {
            throw new UnityException("FindCanvas failed! Check tag of scene Canvas.");
        }
        return canvas;
    }
}