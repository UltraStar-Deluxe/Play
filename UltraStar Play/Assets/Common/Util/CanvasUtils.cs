using UnityEngine;
using UnityEngine.UI;

public class CanvasUtils
{
    public static Canvas FindCanvas()
    {
        return GameObjectUtils.FindComponentWithTag<Canvas>("Canvas");
    }
}