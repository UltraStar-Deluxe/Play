using UnityEngine.UIElements;

public class VisualElementPool<T> : ObjectPool<T>
    where T : VisualElement
{
    public VisualElementPool()
        : base(VisualElementExtensions.HideByDisplay,
               VisualElementExtensions.ShowByDisplay)
    {
    }
}
