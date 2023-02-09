using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SetFirstAndLastChildClassControl
{
    public VisualElement FirstChild { get; private set; }
    public VisualElement LastChild { get; private set; }

    private readonly VisualElement visualElement;

    public SetFirstAndLastChildClassControl(VisualElement visualElement)
    {
        this.visualElement = visualElement;
        visualElement.RegisterCallback<GeometryChangedEvent>(_ => UpdateChildClasses());
        if (Application.isPlaying)
        {
            // Update the firstChild / lastChild classes when the children change
            visualElement.ObserveEveryValueChanged(it => it.Children()
                .Count(child => child.IsVisibleByDisplay()))
                .Subscribe(_ => UpdateChildClasses());
        }
    }

    public void UpdateChildClasses()
    {
        VisualElement newFirstChild = visualElement.Children()
            .FirstOrDefault(child => child.IsVisibleByDisplay());
        if (newFirstChild != FirstChild)
        {
            newFirstChild?.AddToClassList("firstChild");
            FirstChild?.RemoveFromClassList("firstChild");
            FirstChild = newFirstChild;
        }
        
        VisualElement newLastChild = visualElement.Children()
            .LastOrDefault(child => child.IsVisibleByDisplay());
        if (newLastChild != LastChild)
        {
            newLastChild?.AddToClassList("lastChild");
            LastChild?.RemoveFromClassList("lastChild");
            LastChild = newLastChild;
        }
    }
}
