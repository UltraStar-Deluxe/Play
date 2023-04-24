using System;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class ReusableCollectionItem
{
    public const int UndefinedIndex = -1;
    protected EventCallback<GeometryChangedEvent> m_GeometryChangedEventCallback;

    public virtual VisualElement rootElement => this.bindableElement;

    public VisualElement bindableElement { get; protected set; }

    public ValueAnimation<StyleValues> animator { get; set; }

    public int index { get; set; }

    public int id { get; set; }

    internal bool isDragGhost { get; set; }

    public event Action<ReusableCollectionItem> onGeometryChanged;

    public ReusableCollectionItem()
    {
        this.index = this.id = -1;
        this.m_GeometryChangedEventCallback = new EventCallback<GeometryChangedEvent>(this.OnGeometryChanged);
    }

    public virtual void Init(VisualElement item) => this.bindableElement = item;

    public virtual void PreAttachElement()
    {
        this.rootElement.AddToClassList(BaseVerticalCollectionView.itemUssClassName);
        this.rootElement.RegisterCallback<GeometryChangedEvent>(this.m_GeometryChangedEventCallback);
    }

    public virtual void DetachElement()
    {
        this.rootElement.RemoveFromClassList(BaseVerticalCollectionView.itemUssClassName);
        this.rootElement.UnregisterCallback<GeometryChangedEvent>(this.m_GeometryChangedEventCallback);
        this.rootElement?.RemoveFromHierarchy();
        this.SetSelected(false);
        this.index = this.id = -1;
        this.isDragGhost = false;
    }

    public virtual void SetSelected(bool selected)
    {
        if (selected)
        {
            this.rootElement.AddToClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName);
            // this.rootElement.pseudoStates |= PseudoStates.Checked;
        }
        else
        {
            this.rootElement.RemoveFromClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName);
            // this.rootElement.pseudoStates &= ~PseudoStates.Checked;
        }
    }

    protected void OnGeometryChanged(GeometryChangedEvent evt)
    {
        Action<ReusableCollectionItem> onGeometryChanged = this.onGeometryChanged;
        if (onGeometryChanged == null)
            return;
        onGeometryChanged(this);
    }
}
