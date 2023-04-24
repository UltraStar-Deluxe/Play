// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using System.Collections;
using UnityEngine.UIElements;

/// <summary>
///        <para>
/// A ListView is a horizontally scrollable area that links to, and displays, a list of items.
/// </para>
///      </summary>
public class ListViewH : BaseListViewH
{
    private Func<VisualElement> m_MakeItem;
    private Action<VisualElement, int> m_BindItem;

    /// <summary>
    ///        <para>
    /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
    /// </para>
    ///      </summary>
    public new Func<VisualElement> makeItem
    {
        get => this.m_MakeItem;
        set
        {
            if (!(value != this.m_MakeItem))
                return;
            this.m_MakeItem = value;
            this.Rebuild();
        }
    }

    internal void SetMakeItemWithoutNotify(Func<VisualElement> func) => this.m_MakeItem = func;

    /// <summary>
    ///        <para>
    /// Callback for binding a data item to the visual element.
    /// </para>
    ///      </summary>
    public new Action<VisualElement, int> bindItem
    {
        get => this.m_BindItem;
        set
        {
            if (!(value != this.m_BindItem))
                return;
            this.m_BindItem = value;
            this.RefreshItems();
        }
    }

    internal void SetBindItemWithoutNotify(Action<VisualElement, int> callback) => this.m_BindItem = callback;

    /// <summary>
    ///        <para>
    /// Callback for unbinding a data item from the VisualElement.
    /// </para>
    ///      </summary>
    public new Action<VisualElement, int> unbindItem { get; set; }

    /// <summary>
    ///        <para>
    /// Callback invoked when a VisualElement created via makeItem is no longer needed and will be destroyed.
    /// </para>
    ///      </summary>
    public new Action<VisualElement> destroyItem { get; set; }

    protected override CollectionViewController CreateViewController() =>
        (CollectionViewController)new ListViewHController();

    /// <summary>
    ///        <para>
    /// Creates a ListView with all default properties. The ListView.itemSource
    /// must all be set for the ListView to function properly.
    /// </para>
    ///      </summary>
    public ListViewH() => this.AddToClassList(BaseListView.ussClassName);

    public ListViewH(
        IList itemsSource,
        float itemWidth = -1f,
        Func<VisualElement> makeItem = null,
        Action<VisualElement, int> bindItem = null)
        : base(itemsSource, itemWidth)
    {
        this.AddToClassList(BaseListView.ussClassName);
        this.makeItem = makeItem;
        this.bindItem = bindItem;
    }

    /// <summary>
    ///        <para>
    /// Instantiates a ListView using data from a UXML file.
    /// </para>
    ///      </summary>
    public new class UxmlFactory : UnityEngine.UIElements.UxmlFactory<ListViewH, ListViewH.UxmlTraits>
    {
    }

    /// <summary>
    ///        <para>
    /// Defines UxmlTraits for the ListView.
    /// </para>
    ///      </summary>
    public new class UxmlTraits : BaseListViewH.UxmlTraits
    {
    }
}
