using UnityEngine;
using UnityEngine.UIElements;

public static class VisualElementUtils
{
    public static void MoveVisualElementFullyInsideScreen(VisualElement visualElement, PanelHelper panelHelper)
    {
        Vector2 screenSizePanelCoordinates = panelHelper.ScreenToPanel(new Vector2(Screen.width, Screen.height));
        float overshootRight = visualElement.worldBound.xMax - screenSizePanelCoordinates.x;
        float overshootLeft = visualElement.worldBound.xMin;
        float overshootBottom = visualElement.worldBound.yMax - screenSizePanelCoordinates.y;
        float overshootTop = visualElement.worldBound.yMin;

        Vector2 shift = Vector2.zero;
        if (overshootLeft < 0)
        {
            shift = new Vector2(overshootLeft, shift.y);
        }
        if (overshootRight > 0)
        {
            shift = new Vector2(overshootRight, shift.y);
        }

        if (overshootTop < 0)
        {
            shift = new Vector2(shift.x, overshootTop);
        }
        if (overshootBottom > 0)
        {
            shift = new Vector2(shift.x, overshootBottom);
        }

        if (shift == Vector2.zero)
        {
            return;
        }

        if (shift.x != 0)
        {
            if (visualElement.style.left != new StyleLength(StyleKeyword.Null)
                && visualElement.style.left != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.left = visualElement.style.left.value.value - shift.x;
            }
            else if (visualElement.style.right != new StyleLength(StyleKeyword.Null)
                     && visualElement.style.right != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.right = visualElement.style.right.value.value + shift.x;
            }
        }

        if (shift.y != 0)
        {
            if (visualElement.style.top != new StyleLength(StyleKeyword.Null)
                && visualElement.style.top != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.top = visualElement.style.top.value.value - shift.y;
            }
            else if (visualElement.style.bottom != new StyleLength(StyleKeyword.Null)
                     && visualElement.style.bottom != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.bottom = visualElement.style.bottom.value.value + shift.y;
            }
        }
    }
}
