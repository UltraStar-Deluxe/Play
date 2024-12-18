using UnityEngine.UIElements;

public static class VisualElementTestExtensions
{
    public static void SendClickEvent(this Button button)
    {
        using ClickEvent evt = new ClickEvent()
        {
            target = button
        };
        button.SendEvent(evt);
    }

    public static void SendNavigationSubmitEvent(this VisualElement visualElement)
    {
        visualElement.Focus();
        using NavigationSubmitEvent evt = new NavigationSubmitEvent()
        {
            target = visualElement
        };
        visualElement.SendEvent(evt);
    }

    public static void SendPointerDownEvent(this VisualElement visualElement)
    {
        using PointerDownEvent evt = new PointerDownEvent()
        {
            target = visualElement
        };
        visualElement.SendEvent(evt);
    }
}
