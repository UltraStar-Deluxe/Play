// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

internal class FixedWidthVirtualizationController<T> : HorizontalVirtualizationController<T>
    where T : ReusableCollectionItem, new()
{
    private float resolvedItemWidth => this.m_CollectionView.ResolveItemWidth();

    protected int itemsCount => !this.m_CollectionView.sourceIncludesArraySize
        ? this.m_CollectionView.itemsSource.Count
        : this.m_CollectionView.itemsSource.Count - 1;

    protected override bool VisibleItemPredicate(T i) => true;

    public FixedWidthVirtualizationController(BaseHorizontalCollectionView collectionView)
        : base(collectionView)
    {
    }

    public override int GetIndexFromPosition(Vector2 position) =>
        (int)((double)position.x / (double)this.resolvedItemWidth);

    public override float GetExpectedItemWidth(int index) => this.resolvedItemWidth;

    public override float GetExpectedContentWidth() => (float)this.itemsCount * this.resolvedItemWidth;

    public override void ScrollToItem(int index)
    {
        if (this.visibleItemCount == 0 || index < -1)
            return;
        float resolvedItemWidth = this.resolvedItemWidth;
        if (index == -1)
        {
            if (this.itemsCount < (int)((double)this.lastWidth / (double)resolvedItemWidth))
                this.m_ScrollView.scrollOffset = new Vector2(0.0f, 0.0f);
            else
                this.m_ScrollView.scrollOffset = new Vector2(0.0f, (float)(this.itemsCount + 1) * resolvedItemWidth);
        }
        else if (this.firstVisibleIndex >= index)
        {
            this.m_ScrollView.scrollOffset = Vector2.up * (resolvedItemWidth * (float)index);
        }
        else
        {
            int num1 = (int)((double)this.lastWidth / (double)resolvedItemWidth);
            if (index < this.firstVisibleIndex + num1)
                return;
            int num2 = index - num1 + 1;
            float num3 = resolvedItemWidth - (this.lastWidth - (float)num1 * resolvedItemWidth);
            this.m_ScrollView.scrollOffset =
                new Vector2(resolvedItemWidth * (float)num2 + num3, this.m_ScrollView.scrollOffset.y);
        }
    }

    public override void Resize(Vector2 size)
    {
        float resolvedItemWidth = this.resolvedItemWidth;
        float expectedContentWidth = this.GetExpectedContentWidth();
        this.m_ScrollView.contentContainer.style.width = (StyleLength)expectedContentWidth;
        float num1 = Mathf.Max(0.0f, expectedContentWidth - this.m_ScrollView.contentViewport.layout.width);
        float num2 = Mathf.Min(this.serializedData.scrollOffset.x, num1);

        // TODO: Implement without notify
        // this.m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(num1);
        this.m_ScrollView.horizontalScroller.slider.highValue = num1;

        this.m_ScrollView.horizontalScroller.slider.SetValueWithoutNotify(num2);
        int a = (int)((double)this.m_CollectionView.ResolveItemWidth(size.x) / (double)resolvedItemWidth);
        if (a > 0)
            a += 2;
        int num3 = Mathf.Min(a, this.itemsCount);
        if (this.visibleItemCount != num3)
        {
            int visibleItemCount = this.visibleItemCount;
            if (this.visibleItemCount > num3)
            {
                int num4 = visibleItemCount - num3;
                for (int index = 0; index < num4; ++index)
                    this.ReleaseItem(this.m_ActiveItems.Count - 1);
            }
            else
            {
                int num5 = num3 - this.visibleItemCount;
                for (int index = 0; index < num5; ++index)
                {
                    int newIndex = index + this.firstVisibleIndex + visibleItemCount;
                    this.Setup(this.GetOrMakeItemAtIndex(-1, -1), newIndex);
                }
            }
        }

        this.OnScroll(new Vector2(0.0f, num2));
    }

    public override void OnScroll(Vector2 scrollOffset)
    {
        float x = scrollOffset.x;
        float resolvedItemWidth = this.resolvedItemWidth;
        int num1 = (int)((double)x / (double)resolvedItemWidth);
        this.m_ScrollView.contentContainer.style.paddingLeft = (StyleLength)((float)num1 * resolvedItemWidth);
        this.m_ScrollView.contentContainer.style.width = (StyleLength)((float)this.itemsCount * resolvedItemWidth);
        this.serializedData.scrollOffset.x = scrollOffset.x;
        if (num1 == this.firstVisibleIndex)
            return;
        this.firstVisibleIndex = num1;
        if (this.m_ActiveItems.Count > 0)
        {
            if (this.firstVisibleIndex < this.m_ActiveItems[0].index)
            {
                int num2 = this.m_ActiveItems[0].index - this.firstVisibleIndex;
                List<T> scrollInsertionList = this.m_ScrollInsertionList;
                for (int index = 0; index < num2 && this.m_ActiveItems.Count > 0; ++index)
                {
                    List<T> activeItems = this.m_ActiveItems;
                    T obj = activeItems[activeItems.Count - 1];
                    scrollInsertionList.Add(obj);
                    this.m_ActiveItems.RemoveAt(this.m_ActiveItems.Count - 1);
                    obj.rootElement.SendToBack();
                }

                this.m_ActiveItems.InsertRange(0, (IEnumerable<T>)scrollInsertionList);
                this.m_ScrollInsertionList.Clear();
            }
            else
            {
                int firstVisibleIndex = this.firstVisibleIndex;
                List<T> activeItems = this.m_ActiveItems;
                int index = activeItems[activeItems.Count - 1].index;
                if (firstVisibleIndex < index)
                {
                    List<T> scrollInsertionList = this.m_ScrollInsertionList;
                    int num3 = 0;
                    while (this.firstVisibleIndex > this.m_ActiveItems[num3].index)
                    {
                        T activeItem = this.m_ActiveItems[num3];
                        scrollInsertionList.Add(activeItem);
                        ++num3;
                        activeItem.rootElement.BringToFront();
                    }

                    this.m_ActiveItems.RemoveRange(0, num3);
                    this.m_ActiveItems.AddRange((IEnumerable<T>)scrollInsertionList);
                    scrollInsertionList.Clear();
                }
            }

            for (int index = 0; index < this.m_ActiveItems.Count; ++index)
            {
                int newIndex = index + this.firstVisibleIndex;
                this.Setup(this.m_ActiveItems[index], newIndex);
            }
        }
    }

    internal override T GetOrMakeItemAtIndex(int activeItemIndex = -1, int scrollViewIndex = -1)
    {
        T orMakeItemAtIndex = base.GetOrMakeItemAtIndex(activeItemIndex, scrollViewIndex);
        orMakeItemAtIndex.rootElement.style.width = (StyleLength)this.resolvedItemWidth;
        return orMakeItemAtIndex;
    }

    internal override void EndDrag(int dropIndex)
    {
        this.m_DraggedItem.rootElement.style.width = (StyleLength)this.resolvedItemWidth;
        if (this.firstVisibleIndex > this.m_DraggedItem.index)
            this.m_ScrollView.horizontalScroller.value = this.serializedData.scrollOffset.x - this.resolvedItemWidth;
        base.EndDrag(dropIndex);
    }
}
