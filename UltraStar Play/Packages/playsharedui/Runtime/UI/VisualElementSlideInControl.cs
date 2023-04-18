using System;
using UnityEngine;
using UnityEngine.UIElements;

public class VisualElementSlideInControl
{
    private readonly VisualElement visualElement;
    private readonly ESide2D side;
    private readonly ToggleControl toggleControl;

    private bool isInitialized;

    private Vector2 ResolvedStyleSize => new Vector2(visualElement.resolvedStyle.width, visualElement.resolvedStyle.height);
    private Vector2 lastSize;
    
    public bool Visible
    {
        get => toggleControl.State;
        set => toggleControl.State = value;
    }

    public VisualElementSlideInControl(VisualElement visualElement, ESide2D side, bool initiallyVisible)
    {
        this.visualElement = visualElement;
        this.side = side;
        toggleControl = new ToggleControl(initiallyVisible, SliderIn, SlideOut);

        this.visualElement.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (!isInitialized)
            {
                isInitialized = true;
                if (initiallyVisible)
                {
                    SliderIn();
                }
                else
                {
                    SlideOut();
                }

                lastSize = ResolvedStyleSize;
            }
            else if (Math.Abs(lastSize.x - ResolvedStyleSize.x) > 1f
                     || Math.Abs(lastSize.y - ResolvedStyleSize.y) > 1f)
            {
                lastSize = ResolvedStyleSize;
                UpdatePositionWithoutTransition();
            }
        });
    }

    private void UpdatePositionWithoutTransition()
    {
        // No animation is done when the units change. Here, we change from unit "auto" to unit "px".
        if (side == ESide2D.Right)
        {
            visualElement.style.right = new StyleLength(StyleKeyword.Auto);
        }
        else if (side == ESide2D.Left)
        {
            visualElement.style.left = new StyleLength(StyleKeyword.Auto);
        }
        else if (side == ESide2D.Top)
        {
            visualElement.style.top = new StyleLength(StyleKeyword.Auto);
        }
        else if (side == ESide2D.Bottom)
        {
            visualElement.style.bottom = new StyleLength(StyleKeyword.Auto);
        }
        
        if (Visible)
        {
            SliderIn();
        }
        else
        {
            SlideOut();
        }
    }

    private void SlideOut()
    {
        if (side == ESide2D.Right)
        {
            visualElement.style.right = -visualElement.resolvedStyle.width;
        }
        else if (side == ESide2D.Left)
        {
            visualElement.style.left = -visualElement.resolvedStyle.width;
        }
        else if (side == ESide2D.Top)
        {
            visualElement.style.top = -visualElement.resolvedStyle.height;
        }
        else if (side == ESide2D.Bottom)
        {
            visualElement.style.bottom = -visualElement.resolvedStyle.height;
        }
    }

    private void SliderIn()
    {
        if (side == ESide2D.Right)
        {
            visualElement.style.right = 0;
        }
        else if (side == ESide2D.Left)
        {
            visualElement.style.left = 0;
        }
        else if (side == ESide2D.Top)
        {
            visualElement.style.top = 0;
        }
        else if (side == ESide2D.Bottom)
        {
            visualElement.style.bottom = 0;
        }
    }

    public void ToggleVisible()
    {
        Visible = !Visible;
    }
}
