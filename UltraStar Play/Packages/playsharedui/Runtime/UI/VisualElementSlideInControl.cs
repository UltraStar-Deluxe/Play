using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class VisualElementSlideInControl
{
    public const string SlideOutClassName = "visualElementSlideOut";
    public const string SlideInClassName = "visualElementSlideIn";

    private readonly VisualElement visualElement;
    private readonly ESide2D side;
    private readonly ToggleControl toggleControl;

    private bool isInitialized;

    private Vector2 ResolvedStyleSize => new Vector2(visualElement.resolvedStyle.width, visualElement.resolvedStyle.height);
    private Vector2 lastSize;

    public float OutsideMargin { get; set; } = 2f;
    public ReactiveProperty<bool> Visible => toggleControl.State;

    public VisualElementSlideInControl(VisualElement visualElement, ESide2D side, bool initiallyVisible)
    {
        this.visualElement = visualElement;
        this.side = side;
        toggleControl = new ToggleControl(initiallyVisible, DoSlideIn, DoSlideOut);

        // Element must be visible for USS animation
        visualElement.ShowByDisplay();

        this.visualElement.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            Vector2 resolvedStyleSize = ResolvedStyleSize;

            if (!isInitialized)
            {
                isInitialized = true;
                lastSize = resolvedStyleSize;
                UpdatePositionWithoutTransition();
            }
            else if (Math.Abs(lastSize.x - resolvedStyleSize.x) > 5f
                     || Math.Abs(lastSize.y - resolvedStyleSize.y) > 5f)
            {
                Debug.Log($"VisualElementSlideInControl: size of {visualElement.name} changed from {lastSize} to {resolvedStyleSize}. Thus, updating position without transition.");
                lastSize = resolvedStyleSize;
                UpdatePositionWithoutTransition();
            }
        });
    }

    public void SlideIn()
    {
        Visible.Value = true;
    }

    public void SlideOut()
    {
        Visible.Value = false;
    }

    private void EnableTransition()
    {
        visualElement.style.transitionProperty = new(new List<StylePropertyName> {
            new StylePropertyName("right"),
            new StylePropertyName("left"),
            new StylePropertyName("top"),
            new StylePropertyName("bottom")
        });
    }

    private void DisableTransition()
    {
        visualElement.style.transitionProperty = new(new List<StylePropertyName>());
    }

    public async void UpdatePositionWithoutTransition()
    {
        DisableTransition();

        if (Visible.Value)
        {
            DoSlideIn();
        }
        else
        {
            DoSlideOut();
        }

        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        EnableTransition();
    }

    private void DoSlideOut()
    {
        visualElement.RemoveFromClassList(SlideInClassName);
        visualElement.AddToClassList(SlideOutClassName);

        if (side == ESide2D.Right)
        {
            visualElement.style.right = -(visualElement.resolvedStyle.width + OutsideMargin);
        }
        else if (side == ESide2D.Left)
        {
            visualElement.style.left = -(visualElement.resolvedStyle.width + OutsideMargin);
        }
        else if (side == ESide2D.Top)
        {
            visualElement.style.top = -(visualElement.resolvedStyle.height + OutsideMargin);
        }
        else if (side == ESide2D.Bottom)
        {
            visualElement.style.bottom = -(visualElement.resolvedStyle.height + OutsideMargin);
        }
    }

    private void DoSlideIn()
    {
        visualElement.AddToClassList(SlideInClassName);
        visualElement.RemoveFromClassList(SlideOutClassName);

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
        toggleControl.ToggleState();
    }
}
