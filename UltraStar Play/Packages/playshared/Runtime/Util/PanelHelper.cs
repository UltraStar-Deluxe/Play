using System;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * Utility class to get the rendered pixel size of a VisualElement.
 * See https://forum.unity.com/threads/get-final-rendered-pixel-size-of-visualelement.1030369
 */
public class PanelHelper
{
    private readonly IPanel panel;
    private readonly PanelSettings panelSettings;
    private Vector2 scalingRatio;
    private int cacheFrame;
    private float screenHeight;
    private Vector2 panelScreenMin;

    public PanelHelper(UIDocument document)
        : this(document.rootVisualElement.panel, document.panelSettings)
    {
    }
    
    public PanelHelper(IPanel panel, PanelSettings panelSettings)
    {
        this.panel = panel;
        this.panelSettings = panelSettings;
    }
    
    public Vector2 ScreenToPanel(Vector2 v)
    {
        return RuntimePanelUtils.ScreenToPanel(panel, v);
    }

    public Vector2 PanelToScreen(Vector2 v)
    {
        if (Time.frameCount != cacheFrame)
        {
            UpdateCache();
        }
        Vector2 result = (v - panelScreenMin) * scalingRatio;
        result.y = screenHeight - result.y;
        return result;
    }

    public Rect PanelToScreen(Rect r)
    {
        Vector2 a = PanelToScreen(new Vector2(r.xMin, r.yMin));
        Vector2 b = PanelToScreen(new Vector2(r.xMax, r.yMax));
        Vector2 min = new(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        Vector2 size = new(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        return new Rect(min, size);
    }

    public float GetScaleWeight() => panelSettings.scaleMode switch
    {
        PanelScaleMode.ConstantPixelSize => .5f,
        PanelScaleMode.ConstantPhysicalSize => .5f,
        PanelScaleMode.ScaleWithScreenSize => panelSettings.match,
        _ => throw new ArgumentOutOfRangeException()
    };

    public Vector2 GetScalingRatio()
    {
        if (Time.frameCount != cacheFrame)
        {
            UpdateCache();
        }
        return scalingRatio;
    }

    private void UpdateCache()
    {
        cacheFrame = Time.frameCount;
        Vector2 screenSize = new(Screen.width, Screen.height);
        Vector2 stpMin = ScreenToPanel(new Vector2(0, 0));
        Vector2 stpMax = ScreenToPanel(screenSize);
        Vector2 stpSize = stpMax - stpMin;
        screenHeight = screenSize.y;
        panelScreenMin = stpMin;
        scalingRatio = screenSize / stpSize;
    }

    public Vector2 InvertVertical(Vector2 v)
    {
        return new Vector2(v.x, GetScreenSizeInPanelCoordinates().y - v.y);
    }

    public Vector2 GetScreenSizeInPanelCoordinates()
    {
        return ScreenToPanel(new Vector2(Screen.width, Screen.height));
    }
}
