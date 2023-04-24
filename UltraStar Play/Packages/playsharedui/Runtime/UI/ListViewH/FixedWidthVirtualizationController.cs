using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

internal class FixedWidthVirtualizationController<T> : HorizontalVirtualizationController<T> where T : ReusableCollectionItem, new()
  {
    private float resolvedItemHeight => this.m_CollectionView.ResolveItemHeight();

    protected int itemsCount => !this.m_CollectionView.sourceIncludesArraySize ? this.m_CollectionView.itemsSource.Count : this.m_CollectionView.itemsSource.Count - 1;
    
    protected override bool VisibleItemPredicate(T i) => true;

    public FixedWidthVirtualizationController(BaseHorizontalCollectionView collectionView)
      : base(collectionView)
    {
    }

    public override int GetIndexFromPosition(Vector2 position) => (int) ((double) position.y / (double) this.resolvedItemHeight);

    public override float GetExpectedItemHeight(int index) => this.resolvedItemHeight;

    public override float GetExpectedContentHeight() => (float) this.itemsCount * this.resolvedItemHeight;

    public override void ScrollToItem(int index)
    {
      if (this.visibleItemCount == 0 || index < -1)
        return;
      float resolvedItemHeight = this.resolvedItemHeight;
      if (index == -1)
      {
        if (this.itemsCount < (int) ((double) this.lastHeight / (double) resolvedItemHeight))
          this.m_ScrollView.scrollOffset = new Vector2(0.0f, 0.0f);
        else
          this.m_ScrollView.scrollOffset = new Vector2(0.0f, (float) (this.itemsCount + 1) * resolvedItemHeight);
      }
      else if (this.firstVisibleIndex >= index)
      {
        this.m_ScrollView.scrollOffset = Vector2.up * (resolvedItemHeight * (float) index);
      }
      else
      {
        int num1 = (int) ((double) this.lastHeight / (double) resolvedItemHeight);
        if (index < this.firstVisibleIndex + num1)
          return;
        int num2 = index - num1 + 1;
        float num3 = resolvedItemHeight - (this.lastHeight - (float) num1 * resolvedItemHeight);
        this.m_ScrollView.scrollOffset = new Vector2(this.m_ScrollView.scrollOffset.x, resolvedItemHeight * (float) num2 + num3);
      }
    }

    public override void Resize(Vector2 size)
    {
      float resolvedItemHeight = this.resolvedItemHeight;
      float expectedContentHeight = this.GetExpectedContentHeight();
      this.m_ScrollView.contentContainer.style.height = (StyleLength) expectedContentHeight;
      float num1 = Mathf.Max(0.0f, expectedContentHeight - this.m_ScrollView.contentViewport.layout.height);
      float num2 = Mathf.Min(this.serializedData.scrollOffset.y, num1);
      
      // TODO: Implement without notify
      // this.m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(num1);
      this.m_ScrollView.verticalScroller.slider.highValue = num1;

      this.m_ScrollView.verticalScroller.slider.SetValueWithoutNotify(num2);
      int a = (int) ((double) this.m_CollectionView.ResolveItemHeight(size.y) / (double) resolvedItemHeight);
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
      float y = scrollOffset.y;
      float resolvedItemHeight = this.resolvedItemHeight;
      int num1 = (int) ((double) y / (double) resolvedItemHeight);
      this.m_ScrollView.contentContainer.style.paddingTop = (StyleLength) ((float) num1 * resolvedItemHeight);
      this.m_ScrollView.contentContainer.style.height = (StyleLength) ((float) this.itemsCount * resolvedItemHeight);
      this.serializedData.scrollOffset.y = scrollOffset.y;
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
          this.m_ActiveItems.InsertRange(0, (IEnumerable<T>) scrollInsertionList);
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
            this.m_ActiveItems.AddRange((IEnumerable<T>) scrollInsertionList);
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
      orMakeItemAtIndex.rootElement.style.height = (StyleLength) this.resolvedItemHeight;
      return orMakeItemAtIndex;
    }

    internal override void EndDrag(int dropIndex)
    {
      this.m_DraggedItem.rootElement.style.height = (StyleLength) this.resolvedItemHeight;
      if (this.firstVisibleIndex > this.m_DraggedItem.index)
        this.m_ScrollView.verticalScroller.value = this.serializedData.scrollOffset.y - this.resolvedItemHeight;
      base.EndDrag(dropIndex);
    }
  }
