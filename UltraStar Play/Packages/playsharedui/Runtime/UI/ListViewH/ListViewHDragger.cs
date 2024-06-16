// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ListViewHDragger : DragEventsProcessor
{
    private ListViewHDragger.DragPosition m_LastDragPosition;
    private VisualElement m_DragHoverBar;
    private const int k_AutoScrollAreaSize = 5;
    private const int k_BetweenElementsAreaSize = 5;
    private const int k_PanSpeed = 20;
    private const int k_DragHoverBarWidth = 2;

    protected BaseHorizontalCollectionView targetListView => this.m_Target as BaseHorizontalCollectionView;

    protected ScrollView targetScrollView => this.targetListView.scrollView;

    public ICollectionDragAndDropController dragAndDropController { get; set; }

    public ListViewHDragger(BaseHorizontalCollectionView listView)
        : base((VisualElement)listView)
    {
    }

    protected override bool CanStartDrag(Vector3 pointerPosition)
    {
        if (this.dragAndDropController == null ||
            !this.targetScrollView.contentContainer.worldBound.Contains(pointerPosition))
            return false;
        if (this.targetListView.selectedIndices.Any<int>())
            return this.dragAndDropController.CanStartDrag(this.targetListView.selectedIndices);
        ReusableCollectionItem recycledItem = this.GetRecycledItem(pointerPosition);
        int num;
        if (recycledItem != null)
            num = this.dragAndDropController.CanStartDrag((IEnumerable<int>)new int[1] { recycledItem.index }) ? 1 : 0;
        else
            num = 0;
        return num != 0;
    }

    protected internal override StartDragArgs StartDrag(Vector3 pointerPosition)
    {
        if (this.targetListView.selectedIndices.Any<int>())
            return this.dragAndDropController.SetupDragAndDrop(this.targetListView.selectedIndices);
        ReusableCollectionItem recycledItem = this.GetRecycledItem(pointerPosition);
        if (recycledItem == null)
            return (StartDragArgs)null;
        return this.dragAndDropController.SetupDragAndDrop((IEnumerable<int>)new int[1] { recycledItem.index });
    }

    protected internal override DragVisualMode UpdateDrag(Vector3 pointerPosition)
    {
        ListViewHDragger.DragPosition dragPosition = new ListViewHDragger.DragPosition();
        DragVisualMode visualMode = this.GetVisualMode(pointerPosition, ref dragPosition);
        if (visualMode == DragVisualMode.Rejected)
            this.ClearDragAndDropUI();
        else
            this.ApplyDragAndDropUI(dragPosition);
        return visualMode;
    }

    private DragVisualMode GetVisualMode(
        Vector3 pointerPosition,
        ref ListViewHDragger.DragPosition dragPosition)
    {
        if (this.dragAndDropController == null)
            return DragVisualMode.Rejected;
        this.HandleDragAndScroll((Vector2)pointerPosition);
        return !this.TryGetDragPosition((Vector2)pointerPosition, ref dragPosition)
            ? DragVisualMode.Rejected
            : this.dragAndDropController.HandleDragAndDrop(
                (IListDragAndDropArgs)this.MakeDragAndDropArgs(dragPosition));
    }

    protected internal override void OnDrop(Vector3 pointerPosition)
    {
        ListViewHDragger.DragPosition dragPosition = new ListViewHDragger.DragPosition();
        if (!this.TryGetDragPosition((Vector2)pointerPosition, ref dragPosition))
            return;
        ListDragAndDropArgs args = this.MakeDragAndDropArgs(dragPosition);
        if (this.dragAndDropController.HandleDragAndDrop((IListDragAndDropArgs)args) == DragVisualMode.Rejected)
            return;
        this.dragAndDropController.OnDrop((IListDragAndDropArgs)args);
    }

    internal void HandleDragAndScroll(Vector2 pointerPosition)
    {
        bool flag1 = (double)pointerPosition.x < (double)this.targetScrollView.worldBound.yMin + 5.0;
        bool flag2 = (double)pointerPosition.x > (double)this.targetScrollView.worldBound.yMax - 5.0;
        if (!(flag1 | flag2))
            return;
        Vector2 vector2 = this.targetScrollView.scrollOffset + (flag1 ? Vector2.down : Vector2.up) * 20f;
        ref Vector2 local = ref vector2;
        double x = (double)vector2.x;
        Rect worldBound = this.targetScrollView.contentContainer.worldBound;
        double width1 = (double)worldBound.width;
        worldBound = this.targetScrollView.contentViewport.worldBound;
        double width2 = (double)worldBound.width;
        double max = (double)Mathf.Max(0.0f, (float)(width1 - width2));
        double num = (double)Mathf.Clamp((float)x, 0.0f, (float)max);
        local.x = (float)num;
        this.targetScrollView.scrollOffset = vector2;
    }

    protected void ApplyDragAndDropUI(ListViewHDragger.DragPosition dragPosition)
    {
        if (this.m_LastDragPosition.Equals(dragPosition))
            return;
        if (this.m_DragHoverBar == null)
        {
            this.m_DragHoverBar = new VisualElement();
            this.m_DragHoverBar.AddToClassList(BaseHorizontalCollectionView.dragHoverBarUssClassName);
            this.m_DragHoverBar.style.width = (StyleLength)this.targetListView.localBound.width;
            this.m_DragHoverBar.style.visibility = (StyleEnum<Visibility>)Visibility.Hidden;
            this.m_DragHoverBar.pickingMode = PickingMode.Ignore;
            this.targetListView.RegisterCallback<GeometryChangedEvent>((EventCallback<GeometryChangedEvent>)(e =>
                this.m_DragHoverBar.style.width = (StyleLength)this.targetListView.localBound.width));
            this.targetScrollView.contentViewport.Add(this.m_DragHoverBar);
        }

        this.ClearDragAndDropUI();
        this.m_LastDragPosition = dragPosition;
        switch (dragPosition.dragAndDropPosition)
        {
            case DragAndDropPosition.OverItem:
                dragPosition.recycledItem.rootElement.AddToClassList(BaseHorizontalCollectionView
                    .itemDragHoverUssClassName);
                break;
            case DragAndDropPosition.BetweenItems:
                if (dragPosition.insertAtIndex == 0)
                {
                    this.PlaceHoverBarAt(0.0f);
                    break;
                }

                this.PlaceHoverBarAtElement(
                    (this.targetListView.GetRecycledItemFromIndex(dragPosition.insertAtIndex - 1) ??
                     this.targetListView.GetRecycledItemFromIndex(dragPosition.insertAtIndex)).rootElement);
                break;
            case DragAndDropPosition.OutsideItems:
                ReusableCollectionItem recycledItemFromIndex =
                    this.targetListView.GetRecycledItemFromIndex(this.targetListView.itemsSource.Count - 1);
                if (recycledItemFromIndex != null)
                {
                    this.PlaceHoverBarAtElement(recycledItemFromIndex.rootElement);
                    break;
                }

                bool sourceIncludesArraySize = false; //this.targetListView.sourceIncludesArraySize
                if (sourceIncludesArraySize && this.targetListView.itemsSource.Count > 0)
                {
                    this.PlaceHoverBarAtElement(this.targetListView.GetRecycledItemFromIndex(0).rootElement);
                    break;
                }

                this.PlaceHoverBarAt(0.0f);
                break;
            default:
                throw new ArgumentOutOfRangeException("dragAndDropPosition", (object)dragPosition.dragAndDropPosition,
                    "Unsupported dragAndDropPosition value");
        }
    }

    protected virtual bool TryGetDragPosition(
        Vector2 pointerPosition,
        ref ListViewHDragger.DragPosition dragPosition)
    {
        ReusableCollectionItem recycledItem = this.GetRecycledItem((Vector3)pointerPosition);
        if (recycledItem != null)
        {
            bool sourceIncludesArraySize = false; //this.targetListView.sourceIncludesArraySize 
            if (sourceIncludesArraySize && recycledItem.index == 0)
            {
                dragPosition.insertAtIndex = recycledItem.index + 1;
                dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                return true;
            }

            if ((double)recycledItem.rootElement.worldBound.yMax - (double)pointerPosition.y < 5.0)
            {
                dragPosition.insertAtIndex = recycledItem.index + 1;
                dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                return true;
            }

            if ((double)pointerPosition.y - (double)recycledItem.rootElement.worldBound.yMin > 5.0)
            {
                Vector2 scrollOffset = this.targetScrollView.scrollOffset;
                this.targetScrollView.ScrollTo(recycledItem.rootElement);
                if (scrollOffset != this.targetScrollView.scrollOffset)
                    return this.TryGetDragPosition(pointerPosition, ref dragPosition);
                dragPosition.recycledItem = recycledItem;
                dragPosition.insertAtIndex = recycledItem.index;
                dragPosition.dragAndDropPosition = DragAndDropPosition.OverItem;
                return true;
            }

            dragPosition.insertAtIndex = recycledItem.index;
            dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
            return true;
        }

        if (!this.targetListView.worldBound.Contains(pointerPosition))
            return false;
        dragPosition.dragAndDropPosition = DragAndDropPosition.OutsideItems;
        double y = (double)pointerPosition.y;
        double yMax = (double)this.targetScrollView.contentContainer.worldBound.yMax;
        dragPosition.insertAtIndex = y < yMax ? 0 : this.targetListView.itemsSource.Count;
        return true;
    }

    private ListDragAndDropArgs MakeDragAndDropArgs(ListViewHDragger.DragPosition dragPosition)
    {
        object obj = (object)null;
        ReusableCollectionItem recycledItem = dragPosition.recycledItem;
        if (recycledItem != null)
            obj = this.targetListView.viewController.GetItemForIndex(recycledItem.index);
        return new ListDragAndDropArgs()
        {
            target = obj,
            insertAtIndex = dragPosition.insertAtIndex,
            dragAndDropPosition = dragPosition.dragAndDropPosition,
            dragAndDropData = this.useDragEvents ? DragAndDropUtility.dragAndDrop.data : this.dragAndDropClient.data
        };
    }

    private void PlaceHoverBarAtElement(VisualElement element)
    {
        VisualElement contentViewport = this.targetScrollView.contentViewport;
        this.PlaceHoverBarAt(Mathf.Min(contentViewport.WorldToLocal(element.worldBound).yMax,
            contentViewport.localBound.yMax - 2f));
    }

    private void PlaceHoverBarAt(float left)
    {
        this.m_DragHoverBar.style.left = (StyleLength)left;
        this.m_DragHoverBar.style.visibility = (StyleEnum<Visibility>)Visibility.Visible;
    }

    protected override void ClearDragAndDropUI()
    {
        this.m_LastDragPosition = new ListViewHDragger.DragPosition();
        foreach (ReusableCollectionItem activeItem in this.targetListView.activeItems)
            activeItem.rootElement.RemoveFromClassList(BaseHorizontalCollectionView.itemDragHoverUssClassName);
        if (this.m_DragHoverBar == null)
            return;
        this.m_DragHoverBar.style.visibility = (StyleEnum<Visibility>)Visibility.Hidden;
    }

    protected ReusableCollectionItem GetRecycledItem(Vector3 pointerPosition)
    {
        foreach (ReusableCollectionItem activeItem in this.targetListView.activeItems)
        {
            if (activeItem.rootElement.worldBound.Contains(pointerPosition))
                return activeItem;
        }

        return (ReusableCollectionItem)null;
    }

    public struct DragPosition : IEquatable<ListViewHDragger.DragPosition>
    {
        public int insertAtIndex;
        public ReusableCollectionItem recycledItem;
        public DragAndDropPosition dragAndDropPosition;

        public bool Equals(ListViewHDragger.DragPosition other) => this.insertAtIndex == other.insertAtIndex &&
                                                                  object.Equals((object)this.recycledItem,
                                                                      (object)other.recycledItem) &&
                                                                  this.dragAndDropPosition == other.dragAndDropPosition;

        public override bool Equals(object obj) => obj is ListViewHDragger.DragPosition other && this.Equals(other);

        public override int GetHashCode() =>
            (int)((DragAndDropPosition)((this.insertAtIndex * 397 ^
                                         (this.recycledItem != null ? this.recycledItem.GetHashCode() : 0)) * 397) ^
                  this.dragAndDropPosition);
    }
}
