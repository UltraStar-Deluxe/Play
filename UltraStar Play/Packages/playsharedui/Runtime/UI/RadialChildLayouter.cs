using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class RadialChildLayouter : VisualElement
{
    public new class UxmlFactory : UxmlFactory<RadialChildLayouter, UxmlTraits> {};
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private readonly UxmlFloatAttributeDescription angleOffset = new() { name = "angle-offset", defaultValue = 0};
        private readonly UxmlFloatAttributeDescription angleHighValue = new() { name = "angle-high-value", defaultValue = 360};
        
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            RadialChildLayouter target = ve as RadialChildLayouter;
            target.AngleOffset = angleOffset.GetValueFromBag(bag, cc);
            target.AngleHighValue = angleHighValue.GetValueFromBag(bag, cc);
        }
    }

    private float angleOffset;
    public float AngleOffset
    {
        get => angleOffset;
        set
        {
            angleOffset = value;
            UpdateChildrenPosition();
        }
    }

    private float angleHighValue;
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
