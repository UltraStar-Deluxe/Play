using UnityEngine;
using UnityEngine.UIElements;

public class NoNavigationTargetFoundEvent
{
    public VisualElement FocusableNavigatorRootVisualElement { get; set; }
    public VisualElement FocusedVisualElement { get; set; }
    public Vector2 NavigationDirection { get; set; }

    public override string ToString()
    {
        return $"{{ {nameof(NavigationDirection)}: {NavigationDirection}," +
               $" {nameof(FocusedVisualElement)}: {FocusedVisualElement}," +
               $" {nameof(FocusableNavigatorRootVisualElement)}: {FocusableNavigatorRootVisualElement} }}";
    }
}
