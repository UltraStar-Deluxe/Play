using UnityEngine;

public static class CanvasUtils
{
    public static Canvas FindCanvas()
    {
        return GameObjectUtils.FindComponentWithTag<Canvas>("Canvas");
    }
}