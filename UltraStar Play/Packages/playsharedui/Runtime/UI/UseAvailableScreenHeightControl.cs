using UnityEngine;
using UnityEngine.UIElements;

public class UseAvailableScreenHeightControl
{
    public float MarginInPx { get; set; }
    
    private readonly VisualElement visualElement;

    private readonly bool setHeight;
    private readonly bool setMaxHeight;
    private readonly bool setMinHeight;

    private int lastFrameCount;
    
    public UseAvailableScreenHeightControl(VisualElement visualElement,
        bool setHeight = true,
        bool setMaxHeight = false,
        bool setMinHeight = false)
    {
        this.visualElement = visualElement;
        this.setHeight = setHeight;
        this.setMaxHeight = setMaxHeight;
        this.setMinHeight = setMinHeight;
        visualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (lastFrameCount == Time.frameCount)
        {
            return;
        }
        lastFrameCount = Time.frameCount;
        
        UpdateHeight();
    }

    private void UpdateHeight()
    {
        if (!VisualElementUtils.HasGeometry(visualElement)
            || !VisualElementUtils.HasGeometry(visualElement.parent))
        {
            return;
        }
        
        float availableHeightInPx = -1;
        if (VisualElementUtils.IsNonStyleKeywordValueSet(visualElement.style.top))
        {
            // Top is set => it grows from top to bottom => use available space from top of VisualElement to bottom of the screen.
            availableHeightInPx = visualElement.parent.worldBound.height - visualElement.worldBound.yMin;
        }
        else if (VisualElementUtils.IsNonStyleKeywordValueSet(visualElement.style.bottom))
        {
            // Bottom is set => it grows from bottom to top => use available space from bottom of VisualElement to top the screen.
            availableHeightInPx = visualElement.parent.worldBound.yMax;
        }

        availableHeightInPx -= MarginInPx;
        
        if (availableHeightInPx > 0)
        {
            if (setHeight)
            {
                visualElement.style.height = availableHeightInPx;
            }

            if (setMaxHeight)
            {
                visualElement.style.maxHeight = availableHeightInPx;
            }
            
            if (setMinHeight)
            {
                visualElement.style.minHeight = availableHeightInPx;
            }
        }
    }
}
