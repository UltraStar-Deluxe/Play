// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using UnityEngine.UIElements;

internal class ReusableListViewItem : ReusableCollectionItem
{
    private VisualElement m_Container;
    private VisualElement m_DragHandle;
    private VisualElement m_ItemContainer;

    public override VisualElement rootElement => this.m_Container ?? this.bindableElement;

    public void Init(VisualElement item, bool usesAnimatedDragger)
    {
        this.Init(item);
        this.UpdateHierarchy(new VisualElement() { name = BaseListView.reorderableItemUssClassName },
            this.bindableElement, usesAnimatedDragger);
    }

    protected void UpdateHierarchy(
        VisualElement root,
        VisualElement item,
        bool usesAnimatedDragger)
    {
        if (usesAnimatedDragger)
        {
            if (this.m_Container != null)
                return;
            this.m_Container = root;
            this.m_Container.AddToClassList(BaseListView.reorderableItemUssClassName);
            this.m_DragHandle = new VisualElement() { name = BaseListView.reorderableItemHandleUssClassName };
            this.m_DragHandle.AddToClassList(BaseListView.reorderableItemHandleUssClassName);
            VisualElement child1 = new VisualElement() { name = BaseListView.reorderableItemHandleBarUssClassName };
            child1.AddToClassList(BaseListView.reorderableItemHandleBarUssClassName);
            this.m_DragHandle.Add(child1);
            VisualElement child2 = new VisualElement() { name = BaseListView.reorderableItemHandleBarUssClassName };
            child2.AddToClassList(BaseListView.reorderableItemHandleBarUssClassName);
            this.m_DragHandle.Add(child2);
            this.m_ItemContainer = new VisualElement() { name = BaseListView.reorderableItemContainerUssClassName };
            this.m_ItemContainer.AddToClassList(BaseListView.reorderableItemContainerUssClassName);
            this.m_ItemContainer.Add(item);
            this.m_Container.Add(this.m_DragHandle);
            this.m_Container.Add(this.m_ItemContainer);
        }
        else
        {
            if (this.m_Container == null)
                return;
            this.m_Container.RemoveFromHierarchy();
            this.m_Container = (VisualElement)null;
        }
    }

    public void UpdateDragHandle(bool needsDragHandle)
    {
        if (needsDragHandle)
        {
            if (this.m_DragHandle.parent != null)
                return;
            this.rootElement.Insert(0, this.m_DragHandle);
            this.rootElement.AddToClassList(BaseListView.reorderableItemUssClassName);
        }
        else if (this.m_DragHandle?.parent != null)
        {
            this.m_DragHandle.RemoveFromHierarchy();
            this.rootElement.RemoveFromClassList(BaseListView.reorderableItemUssClassName);
        }
    }

    public override void PreAttachElement()
    {
        base.PreAttachElement();
        this.rootElement.AddToClassList(BaseListView.itemUssClassName);
    }

    public override void DetachElement()
    {
        base.DetachElement();
        this.rootElement.RemoveFromClassList(BaseListView.itemUssClassName);
    }
}
