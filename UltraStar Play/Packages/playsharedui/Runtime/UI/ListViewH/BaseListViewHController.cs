// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

public abstract class BaseListViewHController : CollectionViewController
{
    public event Action itemsSourceSizeChanged;

    public event Action<IEnumerable<int>> itemsAdded;

    public event Action<IEnumerable<int>> itemsRemoved;

    /// <summary>
    ///        <para>
    /// View for this controller, cast as a BaseListView.
    /// </para>
    ///      </summary>
    protected BaseListViewH baseListView => this.view as BaseListViewH;

    internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
    {
        if (!(reusableItem is ReusableListViewItem listItem))
            return;
        listItem.Init(this.MakeItem(),
            this.baseListView.reorderable && this.baseListView.reorderMode == ListViewReorderMode.Animated);
        this.PostInitRegistration(listItem);
    }

    internal void PostInitRegistration(ReusableListViewItem listItem)
    {
        listItem.bindableElement.style.position = (StyleEnum<Position>)Position.Relative;
        listItem.bindableElement.style.flexBasis = (StyleLength)StyleKeyword.Initial;
        listItem.bindableElement.style.marginTop = (StyleLength)0.0f;
        listItem.bindableElement.style.marginBottom = (StyleLength)0.0f;
        listItem.bindableElement.style.marginLeft = (StyleLength)0.0f;
        listItem.bindableElement.style.marginRight = (StyleLength)0.0f;
        listItem.bindableElement.style.flexGrow = (StyleFloat)0.0f;
        listItem.bindableElement.style.flexShrink = (StyleFloat)0.0f;
    }

    internal override void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
    {
        if (reusableItem is ReusableListViewItem reusableListViewItem)
        {
            int num = !this.baseListView.reorderable || this.baseListView.reorderMode != ListViewReorderMode.Animated
                ? 0
                : (this.NeedsDragHandle(index) ? 1 : 0);
            reusableListViewItem.UpdateDragHandle(num != 0);
        }

        base.InvokeBindItem(reusableItem, index);
    }

    /// <summary>
    ///        <para>
    /// Returns whether this item needs a drag handle or not with the Animated drag mode.
    /// </para>
    ///      </summary>
    /// <param name="index">Item index.</param>
    /// <returns>
    ///   <para>Whether or not the drag handle is needed.</para>
    /// </returns>
    public virtual bool NeedsDragHandle(int index) => !this.baseListView.sourceIncludesArraySize || index != 0;

    /// <summary>
    ///        <para>
    /// Adds a certain amount of items at the end of the collection.
    /// </para>
    ///      </summary>
    /// <param name="itemCount">The number of items to add.</param>
    public virtual void AddItems(int itemCount)
    {
        if (itemCount <= 0)
            return;
        this.EnsureItemSourceCanBeResized();
        int count = this.itemsSource.Count;
        List<int> intList = CollectionPool<List<int>, int>.Get();
        try
        {
            if (this.itemsSource.IsFixedSize)
            {
                this.itemsSource = (IList)BaseListViewHController.AddToArray((Array)this.itemsSource, itemCount);
                for (int index = 0; index < itemCount; ++index)
                    intList.Add(count + index);
            }
            else
            {
                for (int index = 0; index < itemCount; ++index)
                {
                    intList.Add(count + index);
                    this.itemsSource.Add((object)null);
                }
            }

            this.RaiseItemsAdded((IEnumerable<int>)intList);
        }
        finally
        {
            CollectionPool<List<int>, int>.Release(intList);
        }

        this.RaiseOnSizeChanged();
    }

    /// <summary>
    ///        <para>
    /// Moves an item in the source.
    /// </para>
    ///      </summary>
    /// <param name="index">The source index.</param>
    /// <param name="newIndex">The destination index.</param>
    public virtual void Move(int index, int newIndex)
    {
        if (this.itemsSource == null)
            return;
        int num1 = Mathf.Min(index, newIndex);
        int num2 = Mathf.Max(index, newIndex);
        if (num1 < 0 || num2 >= this.itemsSource.Count)
            return;
        int dstIndex = newIndex;
        int num3 = newIndex < index ? 1 : -1;
        for (; Mathf.Min(index, newIndex) < Mathf.Max(index, newIndex); newIndex += num3)
            this.Swap(index, newIndex);
        this.RaiseItemIndexChanged(index, dstIndex);
    }

    /// <summary>
    ///        <para>
    /// Removes an item from the source, by index.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    public virtual void RemoveItem(int index)
    {
        List<int> intList = CollectionPool<List<int>, int>.Get();
        try
        {
            intList.Add(index);
            this.RemoveItems(intList);
        }
        finally
        {
            CollectionPool<List<int>, int>.Release(intList);
        }
    }

    public virtual void RemoveItems(List<int> indices)
    {
        this.EnsureItemSourceCanBeResized();
        if (indices == null)
            return;
        indices.Sort();
        this.RaiseItemsRemoved((IEnumerable<int>)indices);
        if (this.itemsSource.IsFixedSize)
        {
            this.itemsSource = (IList)BaseListViewHController.RemoveFromArray((Array)this.itemsSource, indices);
        }
        else
        {
            for (int index = indices.Count - 1; index >= 0; --index)
                this.itemsSource.RemoveAt(indices[index]);
        }

        this.RaiseOnSizeChanged();
    }

    internal virtual void RemoveItems(int itemCount)
    {
        if (itemCount <= 0)
            return;
        int itemsCount = this.GetItemsCount();
        List<int> intList = CollectionPool<List<int>, int>.Get();
        try
        {
            for (int index = itemsCount - itemCount; index < itemsCount; ++index)
                intList.Add(index);
            this.RemoveItems(intList);
        }
        finally
        {
            CollectionPool<List<int>, int>.Release(intList);
        }
    }

    /// <summary>
    ///        <para>
    /// Removes all items from the source.
    /// </para>
    ///      </summary>
    public virtual void ClearItems()
    {
        if (this.itemsSource == null)
            return;
        this.EnsureItemSourceCanBeResized();
        IEnumerable<int> indices = Enumerable.Range(0, this.itemsSource.Count - 1);
        this.itemsSource.Clear();
        this.RaiseItemsRemoved(indices);
        this.RaiseOnSizeChanged();
    }

    /// <summary>
    ///        <para>
    /// Invokes the itemsSourceSizeChanged event.
    /// </para>
    ///      </summary>
    protected void RaiseOnSizeChanged()
    {
        Action sourceSizeChanged = this.itemsSourceSizeChanged;
        if (sourceSizeChanged == null)
            return;
        sourceSizeChanged();
    }

    protected void RaiseItemsAdded(IEnumerable<int> indices)
    {
        Action<IEnumerable<int>> itemsAdded = this.itemsAdded;
        if (itemsAdded == null)
            return;
        itemsAdded(indices);
    }

    protected void RaiseItemsRemoved(IEnumerable<int> indices)
    {
        Action<IEnumerable<int>> itemsRemoved = this.itemsRemoved;
        if (itemsRemoved == null)
            return;
        itemsRemoved(indices);
    }

    private static Array AddToArray(Array source, int itemCount)
    {
        System.Type elementType = source.GetType().GetElementType();
        if (elementType == (System.Type)null)
            throw new InvalidOperationException("Cannot resize source, because its size is fixed.");
        Array instance = Array.CreateInstance(elementType, source.Length + itemCount);
        Array.Copy(source, instance, source.Length);
        return instance;
    }

    private static Array RemoveFromArray(Array source, List<int> indicesToRemove)
    {
        int length = source.Length - indicesToRemove.Count;
        if (length < 0)
            throw new InvalidOperationException("Cannot remove more items than the current count from source.");
        System.Type elementType = source.GetType().GetElementType();
        if (length == 0)
            return Array.CreateInstance(elementType, 0);
        Array instance = Array.CreateInstance(elementType, length);
        int index1 = 0;
        int index2 = 0;
        for (int index3 = 0; index3 < source.Length; ++index3)
        {
            if (index2 < indicesToRemove.Count && indicesToRemove[index2] == index3)
            {
                ++index2;
            }
            else
            {
                instance.SetValue(source.GetValue(index3), index1);
                ++index1;
            }
        }

        return instance;
    }

    private void Swap(int lhs, int rhs)
    {
        object obj = this.itemsSource[lhs];
        this.itemsSource[lhs] = this.itemsSource[rhs];
        this.itemsSource[rhs] = obj;
    }

    private void EnsureItemSourceCanBeResized()
    {
        // TODO: Implement

        // System.Type type = this.itemsSource?.GetType();
        // // ISSUE: explicit non-virtual call
        // bool flag = (object) type != null && __nonvirtual (type.IsArray);
        // if (this.itemsSource == null || this.itemsSource.IsFixedSize && !flag)
        //   throw new InvalidOperationException("Cannot add or remove items from source, because it is null or its size is fixed.");
    }
}
