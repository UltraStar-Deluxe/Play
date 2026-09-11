using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class RadialChildLayouter : VisualElement
{
    private float angleOffset;
    [UxmlAttribute("angle-offset")]
    public float AngleOffset
    {
        get => angleOffset;
        set
        {
            angleOffset = value;
            UpdateChildrenPosition();
        }
    }

    private float angleHighValue = 360;
    [UxmlAttribute("angle-high-value")]
    public float AngleHighValue
    {
        get => angleHighValue;
        set
        {
            angleHighValue = value;
            UpdateChildrenPosition();
        }
    }
    
    private Vector2 lastSize;
    private int lastChildCount;

    public RadialChildLayouter()
    {
        RegisterCallback<GeometryChangedEvent>(_ =>
        {
            if (!VisualElementUtils.HasGeometry(this)
                || (childCount == lastChildCount
                    && Math.Abs(lastSize.x - resolvedStyle.width) < 1f
                    && Math.Abs(lastSize.y - resolvedStyle.height) < 1f))
            {
                return;
            }

            UpdateChildrenPosition();
        });
    }

    public void UpdateChildrenPosition()
    {
        float width = resolvedStyle.width;
        float height = resolvedStyle.height;
        
        List<VisualElement> children = Children().ToList();
        for (int i = 0; i < childCount; i++)
        {
            VisualElement child = children[i];
            if (!VisualElementUtils.HasGeometry(child))
            {
                Debug.LogWarning("No geometry for child " + i + " of " + this + ".");
                continue;
            }
            
            int direction = i % 2 == 0 ? 1 : -1;
            int distanceFromCenter = (int)Math.Ceiling((double)i / 2);
            int centerOutIndex = (childCount / 2) + (distanceFromCenter * direction);
            float angleDegrees = AngleOffset + ((float)centerOutIndex / childCount * AngleHighValue);
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRad) * width / 2 + width / 2;
            float y = Mathf.Sin(angleRad) * height / 2 + height / 2;
            x -= child.resolvedStyle.width / 2;
            y -= child.resolvedStyle.height / 2;
            child.style.left = new StyleLength(x);
            child.style.top = new StyleLength(y);
        }
        
        lastSize = new Vector2(width, height);
        lastChildCount = childCount;
    }
}
