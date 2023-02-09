using System.Linq;
using UnityEngine.UIElements;

public class SetFirstAndLastChildClassControl
{
    public VisualElement FirstChild { get; private set; }
    public VisualElement LastChild { get; private set; }

    private readonly VisualElement visualElement;

    public SetFirstAndLastChildClassControl(VisualElement visualElement)
    {
        this.visualElement = visualElement;
        visualElement.RegisterCallback<GeometryChangedEvent>(UpdateChildClasses);
    }

    private void UpdateChildClasses(GeometryChangedEvent evt)
    {
        VisualElement newFirstChild = visualElement.Children().FirstOrDefault();
        if (newFirstChild != FirstChild)
        {
            newFirstChild?.AddToClassList("firstChild");
            FirstChild?.RemoveFromClassList("firstChild");
            FirstChild = newFirstChild;
        }
        
        VisualElement newLastChild = visualElement.Children().LastOrDefault();
        if (newLastChild != LastChild)
        {
            newLastChild?.AddToClassList("lastChild");
            LastChild?.RemoveFromClassList("lastChild");
            LastChild = newLastChild;
        }
    }
}
