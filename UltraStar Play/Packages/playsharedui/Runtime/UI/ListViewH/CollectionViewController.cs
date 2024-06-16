// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using System.Collections;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

/// <summary>
///        <para>
/// Base collection view controller. View controllers are meant to take care of data virtualized by any BaseHorizontalCollectionView inheritor.
/// </para>
///      </summary>
public abstract class CollectionViewController : IDisposable
{
    private BaseHorizontalCollectionView m_View;
    private IList m_ItemsSource;

    public event Action itemsSourceChanged;

    public event Action<int, int> itemIndexChanged;

    /// <summary>
    ///        <para>
    /// The items source stored in a non-generic list.
    /// </para>
    ///      </summary>
    public virtual IList itemsSource
    {
        get => this.m_ItemsSource;
        set
        {
            if (this.m_ItemsSource == value)
                return;
            this.m_ItemsSource = value;
            this.RaiseItemsSourceChanged();
        }
    }

    /// <summary>
    ///        <para>
    /// Set the itemsSource without raising the itemsSourceChanged event.
    /// </para>
    ///      </summary>
    /// <param name="source">The new source.</param>
    protected void SetItemsSourceWithoutNotify(IList source) => this.m_ItemsSource = source;

    /// <summary>
    ///        <para>
    /// The view for this controller.
    /// </para>
    ///      </summary>
    protected BaseHorizontalCollectionView view => this.m_View;

    /// <summary>
    ///        <para>
    /// Sets the view for this controller.
    /// </para>
    ///      </summary>
    /// <param name="collectionView">The view for this controller. Must not be null.</param>
    public void SetView(BaseHorizontalCollectionView collectionView)
    {
        this.m_View = collectionView;
        this.PrepareView();
        Assert.IsNotNull<BaseHorizontalCollectionView>(this.m_View, "View must not be null.");
    }

    /// <summary>
    ///        <para>
    /// Initialization step once the view is set.
    /// </para>
    ///      </summary>
    protected virtual void PrepareView()
    {
    }

    /// <summary>
    ///        <para>
    /// Called when this controller is not longer needed to provide a way to release resources.
    /// </para>
    ///      </summary>
    public virtual void Dispose()
    {
        this.itemsSourceChanged = (Action)null;
        this.itemIndexChanged = (Action<int, int>)null;
        this.m_View = (BaseHorizontalCollectionView)null;
    }

    /// <summary>
    ///        <para>
    /// Returns the expected item count in the source.
    /// </para>
    ///      </summary>
    /// <returns>
    ///   <para>The item count.</para>
    /// </returns>
    public virtual int GetItemsCount()
    {
        IList itemsSource = this.m_ItemsSource;
        return itemsSource != null ? itemsSource.Count : 0;
    }

    internal virtual int GetItemsMinCount() => this.GetItemsCount();

    /// <summary>
    ///        <para>
    /// Returns the index for the specified id.
    /// </para>
    ///      </summary>
    /// <param name="id">The item id..</param>
    /// <returns>
    ///   <para>The item index.</para>
    /// </returns>
    public virtual int GetIndexForId(int id) => id;

    /// <summary>
    ///        <para>
    /// Returns the id for the specified index.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    /// <returns>
    ///   <para>The item id.</para>
    /// </returns>
    public virtual int GetIdForIndex(int index) => index;

    /// <summary>
    ///        <para>
    /// Returns the item for the specified index.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    /// <returns>
    ///   <para>The object in the source at this index.</para>
    /// </returns>
    public virtual object GetItemForIndex(int index) =>
        this.m_ItemsSource == null || index < 0 || index >= this.m_ItemsSource.Count
            ? (object)null
            : this.m_ItemsSource[index];

    internal virtual void InvokeMakeItem(ReusableCollectionItem reusableItem) => reusableItem.Init(this.MakeItem());

    internal virtual void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
    {
        this.BindItem(reusableItem.bindableElement, index);
        reusableItem.SetSelected(this.m_View.selectedIndices.Contains<int>(index));

        // TODO: Implement
        // reusableItem.rootElement.pseudoStates &= ~PseudoStates.Hover;
    }

    internal virtual void InvokeUnbindItem(ReusableCollectionItem reusableItem, int index) =>
        this.UnbindItem(reusableItem.bindableElement, index);

    internal virtual void InvokeDestroyItem(ReusableCollectionItem reusableItem) =>
        this.DestroyItem(reusableItem.bindableElement);

    /// <summary>
    ///        <para>
    /// Creates a VisualElement to use in the virtualization of the collection view.
    /// </para>
    ///      </summary>
    /// <returns>
    ///   <para>A VisualElement for the row.</para>
    /// </returns>
    protected abstract VisualElement MakeItem();

    /// <summary>
    ///        <para>
    /// Binds a row to an item index.
    /// </para>
    ///      </summary>
    /// <param name="element">The element from that row, created by MakeItem().</param>
    /// <param name="index">The item index.</param>
    protected abstract void BindItem(VisualElement element, int index);

    /// <summary>
    ///        <para>
    /// Unbinds a row to an item index.
    /// </para>
    ///      </summary>
    /// <param name="element">The element from that row, created by MakeItem().</param>
    /// <param name="index">The item index.</param>
    protected abstract void UnbindItem(VisualElement element, int index);

    /// <summary>
    ///        <para>
    /// Destroys a VisualElement when the view is rebuilt or cleared.
    /// </para>
    ///      </summary>
    /// <param name="element">The element being destroyed.</param>
    protected abstract void DestroyItem(VisualElement element);

    /// <summary>
    ///        <para>
    /// Invokes the itemsSourceChanged event.
    /// </para>
    ///      </summary>
    protected void RaiseItemsSourceChanged()
    {
        Action itemsSourceChanged = this.itemsSourceChanged;
        if (itemsSourceChanged == null)
            return;
        itemsSourceChanged();
    }

    /// <summary>
    ///        <para>
    /// Invokes the itemIndexChanged event.
    /// </para>
    ///      </summary>
    /// <param name="srcIndex">The source index.</param>
    /// <param name="dstIndex">The destination index.</param>
    protected void RaiseItemIndexChanged(int srcIndex, int dstIndex)
    {
        Action<int, int> itemIndexChanged = this.itemIndexChanged;
        if (itemIndexChanged == null)
            return;
        itemIndexChanged(srcIndex, dstIndex);
    }
}
