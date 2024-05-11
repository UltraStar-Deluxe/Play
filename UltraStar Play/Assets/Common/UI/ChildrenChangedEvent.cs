using UnityEngine.UIElements;

public class ChildrenChangedEvent
{
    public VisualElement targetParent { get; set; }
    public VisualElement targetChild { get; set; }
    public int previousChildCount { get; set; }
    public int newChildCount { get; set; }
}
