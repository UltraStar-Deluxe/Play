// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

internal abstract class HorizontalVirtualizationController<T> : CollectionVirtualizationController
    where T : ReusableCollectionItem, new()
{
    private readonly UnityEngine.Pool.ObjectPool<T> m_Pool =
        new UnityEngine.Pool.ObjectPool<T>((Func<T>)(() => new T()),
            actionOnRelease: ((Action<T>)(i => i.DetachElement())));

    protected BaseHorizontalCollectionView m_CollectionView;
    protected const int k_ExtraVisibleItems = 2;
    protected List<T> m_ActiveItems;
    protected T m_DraggedItem;
    private int m_LastFocusedElementIndex = -1;
    private List<int> m_LastFocusedElementTreeChildIndexes = new List<int>();
    protected readonly Func<T, bool> m_VisibleItemPredicateDelegate;
    protected List<T> m_ScrollInsertionList = new List<T>();
    private VisualElement m_EmptyRows;

    public override IEnumerable<ReusableCollectionItem> activeItems =>
        (IEnumerable<ReusableCollectionItem>)this.m_ActiveItems;

    internal int itemsCount => !this.m_CollectionView.sourceIncludesArraySize
        ? this.m_CollectionView.itemsSource.Count
        : this.m_CollectionView.itemsSource.Count - 1;

    protected virtual bool VisibleItemPredicate(T i) =>
        i.rootElement.style.display == (StyleEnum<DisplayStyle>)DisplayStyle.Flex;

    internal T firstVisibleItem => this.m_ActiveItems.FirstOrDefault<T>(this.m_VisibleItemPredicateDelegate);

    internal T lastVisibleItem => this.m_ActiveItems.LastOrDefault<T>(this.m_VisibleItemPredicateDelegate);

    public override int visibleItemCount => this.m_ActiveItems.Count<T>(this.m_VisibleItemPredicateDelegate);

    protected SerializedVirtualizationData serializedData => this.m_CollectionView.serializedVirtualizationData;

    public override int firstVisibleIndex
    {
        get => Mathf.Min(this.serializedData.firstVisibleIndex,
            this.m_CollectionView.viewController.GetItemsCount() - 1);
        protected set => this.serializedData.firstVisibleIndex = value;
    }

    protected float lastWidth => this.m_CollectionView.LastWidth;

    protected HorizontalVirtualizationController(BaseHorizontalCollectionView collectionView)
        : base(collectionView.scrollView)
    {
        this.m_CollectionView = collectionView;
        this.m_ActiveItems = new List<T>();
        this.m_VisibleItemPredicateDelegate = new Func<T, bool>(this.VisibleItemPredicate);

        // TODO: Implement
        // this.m_ScrollView.contentContainer.disableClipping = false;
    }

    public override void Refresh(bool rebuild)
    {
        bool flag1 = this.m_CollectionView.HasValidDataAndBindings();
        for (int index = 0; index < this.m_ActiveItems.Count; ++index)
        {
            int newIndex = this.firstVisibleIndex + index;
            T activeItem = this.m_ActiveItems[index];
            bool flag2 = activeItem.rootElement.style.display == (StyleEnum<DisplayStyle>)DisplayStyle.Flex;
            if (rebuild)
            {
                if (flag1)
                    this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)activeItem,
                        activeItem.index);
                this.m_CollectionView.viewController.InvokeDestroyItem((ReusableCollectionItem)activeItem);
                this.m_Pool.Release(activeItem);
            }
            else if (this.m_CollectionView.itemsSource != null && newIndex >= 0 && newIndex < this.itemsCount)
            {
                if (flag1 && flag2)
                {
                    if (activeItem.index != -1)
                        this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)activeItem,
                            activeItem.index);
                    activeItem.index = -1;
                    this.Setup(activeItem, newIndex);
                }
            }
            else if (flag2)
                this.ReleaseItem(index--);
        }

        if (!rebuild)
            return;
        this.m_Pool.Clear();
        this.m_ActiveItems.Clear();
        this.m_ScrollView.Clear();
    }

    protected void Setup(T recycledItem, int newIndex)
    {
        bool isDragGhost = recycledItem.isDragGhost;
        if (this.GetDraggedIndex() == newIndex)
        {
            if (recycledItem.index != -1)
                this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)recycledItem,
                    recycledItem.index);
            recycledItem.isDragGhost = true;
            recycledItem.index = this.m_DraggedItem.index;
            recycledItem.rootElement.style.maxWidth = (StyleLength)0.0f;
            recycledItem.rootElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.Flex;
            recycledItem.bindableElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.None;
        }
        else
        {
            if (isDragGhost)
            {
                recycledItem.isDragGhost = false;
                recycledItem.rootElement.style.maxWidth = (StyleLength)StyleKeyword.Null;
                recycledItem.bindableElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.Flex;
            }

            if (newIndex >= this.itemsCount)
            {
                recycledItem.rootElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.None;
                if (recycledItem.index < 0 || recycledItem.index >= this.itemsCount)
                    return;
                this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)recycledItem,
                    recycledItem.index);
                recycledItem.index = -1;
            }
            else
            {
                recycledItem.rootElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.Flex;
                if (recycledItem.index == newIndex)
                    return;
                bool enable = this.m_CollectionView.showAlternatingRowBackgrounds != AlternatingRowBackground.None &&
                              newIndex % 2 == 1;
                recycledItem.rootElement.EnableInClassList(
                    BaseHorizontalCollectionView.itemAlternativeBackgroundUssClassName, enable);
                int index = recycledItem.index;
                int idForIndex = this.m_CollectionView.viewController.GetIdForIndex(newIndex);
                if (recycledItem.index != -1)
                    this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)recycledItem,
                        recycledItem.index);
                recycledItem.index = newIndex;
                recycledItem.id = idForIndex;
                int key = newIndex - this.firstVisibleIndex;
                if (key >= this.m_ScrollView.contentContainer.childCount)
                    recycledItem.rootElement.BringToFront();
                else if (key >= 0)
                    recycledItem.rootElement.PlaceBehind(this.m_ScrollView.contentContainer[key]);
                else
                    recycledItem.rootElement.SendToBack();
                this.m_CollectionView.viewController.InvokeBindItem((ReusableCollectionItem)recycledItem, newIndex);
                this.HandleFocus((ReusableCollectionItem)recycledItem, index);
            }
        }
    }

    public override void OnFocus(VisualElement leafTarget)
    {
        if (leafTarget == this.m_ScrollView.contentContainer)
            return;
        this.m_LastFocusedElementTreeChildIndexes.Clear();
        if (this.m_ScrollView.contentContainer.FindElementInTree(leafTarget, this.m_LastFocusedElementTreeChildIndexes))
        {
            VisualElement visualElement =
                this.m_ScrollView.contentContainer[this.m_LastFocusedElementTreeChildIndexes[0]];
            foreach (ReusableCollectionItem activeItem in this.activeItems)
            {
                if (activeItem.rootElement == visualElement)
                {
                    this.m_LastFocusedElementIndex = activeItem.index;
                    break;
                }
            }

            this.m_LastFocusedElementTreeChildIndexes.RemoveAt(0);
        }
        else
            this.m_LastFocusedElementIndex = -1;
    }

    public override void OnBlur(VisualElement willFocus)
    {
        if (willFocus != null && willFocus == this.m_ScrollView.contentContainer)
            return;
        this.m_LastFocusedElementTreeChildIndexes.Clear();
        this.m_LastFocusedElementIndex = -1;
    }

    private void HandleFocus(ReusableCollectionItem recycledItem, int previousIndex)
    {
        if (this.m_LastFocusedElementIndex == -1)
            return;
        if (this.m_LastFocusedElementIndex == recycledItem.index)
            recycledItem.rootElement.ElementAtTreePath(this.m_LastFocusedElementTreeChildIndexes)?.Focus();
        else if (this.m_LastFocusedElementIndex != previousIndex)
            recycledItem.rootElement.ElementAtTreePath(this.m_LastFocusedElementTreeChildIndexes)?.Blur();
        else
            this.m_ScrollView.contentContainer.Focus();
    }

    public override void UpdateBackground()
    {
        float num1;
        if (this.m_CollectionView.showAlternatingRowBackgrounds != AlternatingRowBackground.All ||
            (double)(num1 = this.m_ScrollView.contentViewport.resolvedStyle.width - this.GetExpectedContentWidth()) <=
            0.0)
        {
            this.m_EmptyRows?.RemoveFromHierarchy();
        }
        else
        {
            if ((object)this.lastVisibleItem == null)
                return;
            if (this.m_EmptyRows == null)
            {
                this.m_EmptyRows = new VisualElement();
                this.m_EmptyRows.AddToClassList(BaseHorizontalCollectionView.backgroundFillUssClassName);
            }

            if (this.m_EmptyRows.parent == null)
                this.m_ScrollView.contentViewport.Add(this.m_EmptyRows);
            float expectedItemWidth = this.GetExpectedItemWidth(-1);
            int num2 = Mathf.FloorToInt(num1 / expectedItemWidth) + 1;
            if (num2 > this.m_EmptyRows.childCount)
            {
                int num3 = num2 - this.m_EmptyRows.childCount;
                for (int index = 0; index < num3; ++index)
                    this.m_EmptyRows.Add(new VisualElement() { style = { flexShrink = (StyleFloat)0.0f } });
            }


            // ISSUE: variable of a boxed type
            // TODO: Implement?
            // __Boxed<T> lastVisibleItem = (object)this.lastVisibleItem;
            int num4 = lastVisibleItem != null ? lastVisibleItem.index : -1;
            VisualElement.Hierarchy hierarchy = this.m_EmptyRows.hierarchy;
            int childCount = hierarchy.childCount;
            for (int key = 0; key < childCount; ++key)
            {
                hierarchy = this.m_EmptyRows.hierarchy;
                VisualElement visualElement = hierarchy[key];
                ++num4;
                visualElement.style.width = (StyleLength)expectedItemWidth;
                visualElement.EnableInClassList(BaseHorizontalCollectionView.itemAlternativeBackgroundUssClassName,
                    num4 % 2 == 1);
            }
        }
    }

    internal override void StartDragItem(ReusableCollectionItem item)
    {
        this.m_DraggedItem = item as T;
        int num = this.m_ActiveItems.IndexOf(this.m_DraggedItem);
        this.m_ActiveItems.RemoveAt(num);
        this.Setup(this.GetOrMakeItemAtIndex(num, num), this.m_DraggedItem.index);
    }

    internal override void EndDrag(int dropIndex)
    {
        ReusableCollectionItem recycledItemFromIndex = this.m_CollectionView.GetRecycledItemFromIndex(dropIndex);
        int index1 = recycledItemFromIndex != null
            ? this.m_ScrollView.IndexOf(recycledItemFromIndex.rootElement)
            : this.m_ActiveItems.Count;
        this.m_ScrollView.Insert(index1, this.m_DraggedItem.rootElement);
        this.m_ActiveItems.Insert(index1, this.m_DraggedItem);
        for (int index2 = 0; index2 < this.m_ActiveItems.Count; ++index2)
        {
            T activeItem = this.m_ActiveItems[index2];
            if (activeItem.isDragGhost)
            {
                activeItem.index = -1;
                activeItem.bindableElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.Flex;
                this.ReleaseItem(index2);
                --index2;
            }
        }

        if (dropIndex != this.m_DraggedItem.index)
        {
            if ((object)this.lastVisibleItem != null)
                this.lastVisibleItem.rootElement.style.display = (StyleEnum<DisplayStyle>)DisplayStyle.None;
            if (this.m_DraggedItem.index < dropIndex)
            {
                this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)this.m_DraggedItem,
                    this.m_DraggedItem.index);
                this.m_DraggedItem.index = -1;
            }
            else if (recycledItemFromIndex != null)
            {
                this.m_CollectionView.viewController.InvokeUnbindItem(recycledItemFromIndex,
                    recycledItemFromIndex.index);
                recycledItemFromIndex.index = -1;
            }
        }

        this.m_DraggedItem = default(T);
    }

    internal virtual T GetOrMakeItemAtIndex(int activeItemIndex = -1, int scrollViewIndex = -1)
    {
        T reusableItem = this.m_Pool.Get();
        if (reusableItem.rootElement == null)
            this.m_CollectionView.viewController.InvokeMakeItem((ReusableCollectionItem)reusableItem);
        reusableItem.PreAttachElement();
        if (activeItemIndex == -1)
            this.m_ActiveItems.Add(reusableItem);
        else
            this.m_ActiveItems.Insert(activeItemIndex, reusableItem);
        if (scrollViewIndex == -1)
            this.m_ScrollView.Add(reusableItem.rootElement);
        else
            this.m_ScrollView.Insert(scrollViewIndex, reusableItem.rootElement);
        return reusableItem;
    }

    internal virtual void ReleaseItem(int activeItemsIndex)
    {
        T activeItem = this.m_ActiveItems[activeItemsIndex];
        if (activeItem.index != -1)
            this.m_CollectionView.viewController.InvokeUnbindItem((ReusableCollectionItem)activeItem, activeItem.index);
        this.m_Pool.Release(activeItem);
        this.m_ActiveItems.Remove(activeItem);
    }

    // TODO: Implement
    // protected int GetDraggedIndex() =>
    //     this.m_CollectionView.dragger is ListViewDraggerAnimated dragger && dragger.isDragging
    //         ? dragger.draggedItem.index
    //         : -1;
    protected int GetDraggedIndex() => -1;
}
