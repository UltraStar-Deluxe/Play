// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

/// <summary>
///        <para>
/// Base class for controls that display virtualized vertical content inside a scroll view.
/// </para>
///      </summary>
public abstract class BaseHorizontalCollectionView : BindableElement, ISerializationCallbackReceiver
{
    private SelectionType m_SelectionType;
    public static readonly List<ReusableCollectionItem> k_EmptyItems = new List<ReusableCollectionItem>();
    private bool mVerticalScrollingEnabled;
    [SerializeField] private AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;
    internal static readonly int s_DefaultItemWidth = 22;
    internal float m_FixedItemWidth = (float)BaseHorizontalCollectionView.s_DefaultItemWidth;
    internal bool m_ItemWidthIsInline;
    private CollectionVirtualizationMethod m_VirtualizationMethod;
    private readonly ScrollView m_ScrollView;
    private CollectionViewController m_ViewController;
    private CollectionVirtualizationController m_VirtualizationController;
    private KeyboardNavigationManipulator m_NavigationManipulator;

    [SerializeField]
    internal SerializedVirtualizationData serializedVirtualizationData = new SerializedVirtualizationData();

    [SerializeField] private readonly List<int> m_SelectedIds = new List<int>();
    private readonly List<int> m_SelectedIndices = new List<int>();
    private readonly List<object> m_SelectedItems = new List<object>();
    private float m_LastWidth;
    private float m_LastHeight;
    private bool m_IsRangeSelectionDirectionUp;
    private ListViewHDragger mHDragger;
    internal const float ItemWidthUnset = -1f;
    internal static CustomStyleProperty<int> s_ItemWidthProperty = new CustomStyleProperty<int>("--unity-item-width");
    private Action<int, int> m_ItemIndexChangedCallback;
    private Action m_ItemsSourceChangedCallback;

    /// <summary>
    ///        <para>
    /// The USS class name for BaseVerticalCollectionView elements.
    /// </para>
    ///      </summary>
    public static readonly string ussClassName = "unity-collection-view";

    /// <summary>
    ///        <para>
    /// The USS class name for BaseVerticalCollectionView elements with a border.
    /// </para>
    ///      </summary>
    public static readonly string borderUssClassName = BaseHorizontalCollectionView.ussClassName + "--with-border";

    /// <summary>
    ///        <para>
    /// The USS class name of item elements in BaseVerticalCollectionView elements.
    /// </para>
    ///      </summary>
    public static readonly string itemUssClassName = BaseHorizontalCollectionView.ussClassName + "__item";

    /// <summary>
    ///        <para>
    /// The USS class name of the drag hover bar.
    /// </para>
    ///      </summary>
    public static readonly string dragHoverBarUssClassName =
        BaseHorizontalCollectionView.ussClassName + "__drag-hover-bar";

    /// <summary>
    ///        <para>
    /// The USS class name applied to an item element on drag hover.
    /// </para>
    ///      </summary>
    public static readonly string itemDragHoverUssClassName =
        BaseHorizontalCollectionView.itemUssClassName + "--drag-hover";

    /// <summary>
    ///        <para>
    /// The USS class name of selected item elements in the BaseHorizontalCollectionView.
    /// </para>
    ///      </summary>
    public static readonly string itemSelectedVariantUssClassName =
        BaseHorizontalCollectionView.itemUssClassName + "--selected";

    /// <summary>
    ///        <para>
    /// The USS class name for odd rows in the BaseHorizontalCollectionView.
    /// </para>
    ///      </summary>
    public static readonly string itemAlternativeBackgroundUssClassName =
        BaseHorizontalCollectionView.itemUssClassName + "--alternative-background";

    /// <summary>
    ///        <para>
    /// The USS class name of the scroll view in the BaseHorizontalCollectionView.
    /// </para>
    ///      </summary>
    public static readonly string listScrollViewUssClassName =
        BaseHorizontalCollectionView.ussClassName + "__scroll-view";

    internal static readonly string backgroundFillUssClassName =
        BaseHorizontalCollectionView.ussClassName + "__background-fill";

    private Vector3 m_TouchDownPosition;

    [Obsolete("onItemsChosen is deprecated, use itemsChosen instead", false)]
    public event Action<IEnumerable<object>> onItemsChosen
    {
        add => this.itemsChosen += value;
        remove => this.itemsChosen -= value;
    }

    public event Action<IEnumerable<object>> itemsChosen;

    [Obsolete("onSelectionChange is deprecated, use selectionChanged instead", false)]
    public event Action<IEnumerable<object>> onSelectionChange
    {
        add => this.selectionChanged += value;
        remove => this.selectionChanged -= value;
    }

    public event Action<IEnumerable<object>> selectionChanged;

    [Obsolete("onSelectedIndicesChange is deprecated, use selectedIndicesChanged instead", false)]
    public event Action<IEnumerable<int>> onSelectedIndicesChange
    {
        add => this.selectedIndicesChanged += value;
        remove => this.selectedIndicesChanged -= value;
    }

    public event Action<IEnumerable<int>> selectedIndicesChanged;

    public event Action<int, int> itemIndexChanged;

    public event Action itemsSourceChanged;

    internal event Action selectionNotChanged;

    /// <summary>
    ///        <para>
    /// The data source for collection items.
    /// </para>
    ///      </summary>
    public IList itemsSource
    {
        get => this.viewController?.itemsSource;
        set => this.GetOrCreateViewController().itemsSource = value;
    }

    internal virtual bool sourceIncludesArraySize => false;

    /// <summary>
    ///        <para>
    /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
    /// </para>
    ///      </summary>
    [Obsolete("makeItem has been moved to ListView and TreeView. Use these ones instead.")]
    public Func<VisualElement> makeItem
    {
        get => throw new UnityException("makeItem has been moved to ListView and TreeView. Use these ones instead.");
        set => throw new UnityException("makeItem has been moved to ListView and TreeView. Use these ones instead.");
    }

    /// <summary>
    ///        <para>
    /// Callback for binding a data item to the visual element.
    /// </para>
    ///      </summary>
    [Obsolete("bindItem has been moved to ListView and TreeView. Use these ones instead.")]
    public Action<VisualElement, int> bindItem
    {
        get => throw new UnityException("bindItem has been moved to ListView and TreeView. Use these ones instead.");
        set => throw new UnityException("bindItem has been moved to ListView and TreeView. Use these ones instead.");
    }

    /// <summary>
    ///        <para>
    /// Callback for unbinding a data item from the VisualElement.
    /// </para>
    ///      </summary>
    [Obsolete("unbindItem has been moved to ListView and TreeView. Use these ones instead.")]
    public Action<VisualElement, int> unbindItem
    {
        get => throw new UnityException("unbindItem has been moved to ListView and TreeView. Use these ones instead.");
        set => throw new UnityException("unbindItem has been moved to ListView and TreeView. Use these ones instead.");
    }

    /// <summary>
    ///        <para>
    /// Callback invoked when a VisualElement created via makeItem is no longer needed and will be destroyed.
    /// </para>
    ///      </summary>
    [Obsolete("destroyItem has been moved to ListView and TreeView. Use these ones instead.")]
    public Action<VisualElement> destroyItem
    {
        get => throw new UnityException("destroyItem has been moved to ListView and TreeView. Use these ones instead.");
        set => throw new UnityException("destroyItem has been moved to ListView and TreeView. Use these ones instead.");
    }

    /// <summary>
    ///        <para>
    /// Returns the content container for the BaseHorizontalCollectionView. Because the BaseVerticalCollectionView
    /// control automatically manages its content, this always returns null.
    /// </para>
    ///      </summary>
    public override VisualElement contentContainer => (VisualElement)null;

    /// <summary>
    ///        <para>
    /// Controls the selection type.
    /// </para>
    ///      </summary>
    public SelectionType selectionType
    {
        get => this.m_SelectionType;
        set
        {
            this.m_SelectionType = value;
            if (this.m_SelectionType == SelectionType.None)
            {
                this.ClearSelection();
            }
            else
            {
                if (this.m_SelectionType != SelectionType.Single || this.m_SelectedIndices.Count <= 1)
                    return;
                this.SetSelection(this.m_SelectedIndices.First<int>());
            }
        }
    }

    /// <summary>
    ///        <para>
    /// Returns the selected item from the data source. If multiple items are selected, returns the first selected item.
    /// </para>
    ///      </summary>
    public object selectedItem => this.m_SelectedItems.Count != 0 ? this.m_SelectedItems.First<object>() : (object)null;

    /// <summary>
    ///        <para>
    /// Returns the selected items from the data source. Always returns an enumerable, even if no item is selected, or a single
    /// item is selected.
    /// </para>
    ///      </summary>
    public IEnumerable<object> selectedItems => (IEnumerable<object>)this.m_SelectedItems;

    /// <summary>
    ///        <para>
    /// Returns or sets the selected item's index in the data source. If multiple items are selected, returns the
    /// first selected item's index. If multiple items are provided, sets them all as selected.
    /// </para>
    ///      </summary>
    public int selectedIndex
    {
        get => this.m_SelectedIndices.Count == 0 ? -1 : this.m_SelectedIndices.First<int>();
        set => this.SetSelection(value);
    }

    /// <summary>
    ///        <para>
    /// Returns the indices of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
    /// single item is selected.
    /// </para>
    ///      </summary>
    public IEnumerable<int> selectedIndices => (IEnumerable<int>)this.m_SelectedIndices;

    internal List<int> currentSelectionIds => this.m_SelectedIds;

    internal IEnumerable<ReusableCollectionItem> activeItems => this.m_VirtualizationController?.activeItems ??
                                                                (IEnumerable<ReusableCollectionItem>)
                                                                new List<
                                                                    ReusableCollectionItem>(); //BaseHorizontalCollectionView.k_EmptyItems;

    internal ScrollView scrollView => this.m_ScrollView;

    internal ListViewHDragger HDragger => this.mHDragger;

    internal CollectionVirtualizationController virtualizationController => this.GetOrCreateVirtualizationController();

    /// <summary>
    ///        <para>
    /// The view controller for this view.
    /// </para>
    ///      </summary>
    public CollectionViewController viewController => this.m_ViewController;

    internal float ResolveItemWidth(float width = -1f)
    {
        float scaledPixelsPerPoint = 1; // this.scaledPixelsPerPoint;
        width = (double)width < 0.0 ? this.fixedItemWidth : width;
        return Mathf.Round(width * scaledPixelsPerPoint) / scaledPixelsPerPoint;
    }

    /// <summary>
    ///        <para>
    /// Enable this property to display a border around the collection view.
    /// </para>
    ///      </summary>
    public bool showBorder
    {
        get => this.m_ScrollView.ClassListContains(BaseHorizontalCollectionView.borderUssClassName);
        set => this.m_ScrollView.EnableInClassList(BaseHorizontalCollectionView.borderUssClassName, value);
    }

    /// <summary>
    ///        <para>
    /// Gets or sets a value that indicates whether the user can drag list items to reorder them.
    /// </para>
    ///      </summary>
    public bool reorderable
    {
        // TODO: Implement
        get => false;
        set
        {
            // Do nothing
        }
    }

    /// <summary>
    ///        <para>
    /// This property controls whether the collection view shows a vertical scroll bar when its content
    /// does not fit in the visible area.
    /// </para>
    ///      </summary>
    public bool verticalScrollingEnabled
    {
        get => this.mVerticalScrollingEnabled;
        set
        {
            // if (this.mVerticalScrollingEnabled == value)
            //     return;
            this.mVerticalScrollingEnabled = value;
            this.m_ScrollView.verticalScrollerVisibility =
                value ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            this.m_ScrollView.mode = value ? ScrollViewMode.VerticalAndHorizontal : ScrollViewMode.Horizontal;
        }
    }

    /// <summary>
    ///        <para>
    /// This property controls whether the background colors of collection view rows alternate.
    /// Takes a value from the AlternatingRowBackground enum.
    /// </para>
    ///      </summary>
    public AlternatingRowBackground showAlternatingRowBackgrounds
    {
        get => this.m_ShowAlternatingRowBackgrounds;
        set
        {
            if (this.m_ShowAlternatingRowBackgrounds == value)
                return;
            this.m_ShowAlternatingRowBackgrounds = value;
            this.RefreshItems();
        }
    }

    /// <summary>
    ///        <para>
    /// The virtualization method to use for this collection when a scroll bar is visible.
    /// Takes a value from the CollectionVirtualizationMethod enum.
    /// </para>
    ///      </summary>
    public CollectionVirtualizationMethod virtualizationMethod
    {
        get => this.m_VirtualizationMethod;
        set
        {
            CollectionVirtualizationMethod virtualizationMethod = this.m_VirtualizationMethod;
            this.m_VirtualizationMethod = value;
            if (virtualizationMethod == value)
                return;
            this.CreateVirtualizationController();
            this.Rebuild();
        }
    }

    /// <summary>
    ///        <para>
    /// The width of a single item in the list, in pixels.
    /// </para>
    ///      </summary>
    public float fixedItemWidth
    {
        get => this.m_FixedItemWidth;
        set
        {
            if ((double)value < 0.0)
                throw new ArgumentOutOfRangeException(nameof(fixedItemWidth),
                    "Value needs to be positive for virtualization.");
            this.m_ItemWidthIsInline = true;
            if ((double)Math.Abs(this.m_FixedItemWidth - value) <= 1.401298464324817E-45)
                return;
            this.m_FixedItemWidth = value;
            this.RefreshItems();
        }
    }

    internal float LastWidth => this.m_LastWidth;

    private protected virtual void CreateVirtualizationController() =>
        this.CreateVirtualizationController<ReusableCollectionItem>();

    internal CollectionVirtualizationController GetOrCreateVirtualizationController()
    {
        if (this.m_VirtualizationController == null)
            this.CreateVirtualizationController();
        return this.m_VirtualizationController;
    }

    internal void CreateVirtualizationController<T>() where T : ReusableCollectionItem, new()
    {
        switch (this.virtualizationMethod)
        {
            case CollectionVirtualizationMethod.Fixed:
                this.m_VirtualizationController =
                    (CollectionVirtualizationController)new FixedWidthVirtualizationController<T>(this);
                break;
            case CollectionVirtualizationMethod.Dynamic:
                throw new NotImplementedException("Dynamic not implemented");
                // TODO: Implement
                // this.m_VirtualizationController =
                //     (CollectionVirtualizationController)new DynamicHeightVirtualizationController<T>(this);
                break;
            default:
                throw new ArgumentOutOfRangeException("virtualizationMethod", (object)this.virtualizationMethod,
                    "Unsupported virtualizationMethod virtualization");
        }
    }

    internal CollectionViewController GetOrCreateViewController()
    {
        if (this.m_ViewController == null)
            this.SetViewController(this.CreateViewController());
        return this.m_ViewController;
    }

    /// <summary>
    ///        <para>
    /// Creates the view controller for this view.
    /// Override this method in inheritors to change the controller type.
    /// </para>
    ///      </summary>
    /// <returns>
    ///   <para>The view controller.</para>
    /// </returns>
    protected abstract CollectionViewController CreateViewController();

    /// <summary>
    ///        <para>
    /// Assigns the view controller for this view and registers all events required for it to function properly.
    /// </para>
    ///      </summary>
    /// <param name="controller">The controller to use with this view.</param>
    public virtual void SetViewController(CollectionViewController controller)
    {
        if (this.m_ViewController != null)
        {
            this.m_ViewController.itemIndexChanged -= this.m_ItemIndexChangedCallback;
            this.m_ViewController.itemsSourceChanged -= this.m_ItemsSourceChangedCallback;
            this.m_ViewController.Dispose();
            this.m_ViewController = (CollectionViewController)null;
        }

        this.m_ViewController = controller;
        if (this.m_ViewController == null)
            return;
        this.m_ViewController.SetView(this);
        this.m_ViewController.itemIndexChanged += this.m_ItemIndexChangedCallback;
        this.m_ViewController.itemsSourceChanged += this.m_ItemsSourceChangedCallback;
    }

    internal virtual ListViewHDragger CreateDragger() => new ListViewHDragger(this);

    internal void InitializeDragAndDropController(bool enableReordering)
    {
        if (this.mHDragger != null)
        {
            this.mHDragger.UnregisterCallbacksFromTarget(true);
            this.mHDragger.dragAndDropController = (ICollectionDragAndDropController)null;
            this.mHDragger = (ListViewHDragger)null;
        }

        this.mHDragger = this.CreateDragger();
        this.mHDragger.dragAndDropController = this.CreateDragAndDropController();
        // this.m_Dragger.dragAndDropController.enableReordering = enableReordering;
    }

    internal abstract ICollectionDragAndDropController CreateDragAndDropController();

    internal void SetDragAndDropController(
        ICollectionDragAndDropController dragAndDropController)
    {
        if (this.mHDragger == null)
            this.mHDragger = this.CreateDragger();
        this.mHDragger.dragAndDropController = dragAndDropController;
    }

    /// <summary>
    ///        <para>
    /// Creates a BaseHorizontalCollectionView with all default properties.
    /// The BaseHorizontalCollectionView.itemsSource must all be set for the BaseHorizontalCollectionView to function properly.
    /// </para>
    ///      </summary>
    public BaseHorizontalCollectionView()
    {
        this.AddToClassList(BaseHorizontalCollectionView.ussClassName);
        this.selectionType = SelectionType.Single;
        this.m_ScrollView = new ScrollView();
        this.m_ScrollView.AddToClassList(BaseHorizontalCollectionView.listScrollViewUssClassName);
        this.m_ScrollView.horizontalScroller.valueChanged += (Action<float>)(h => this.OnScroll(new Vector2(h, 0)));
        this.m_ScrollView.RegisterCallback<GeometryChangedEvent>(
            new EventCallback<GeometryChangedEvent>(this.OnSizeChanged));
        this.RegisterCallback<CustomStyleResolvedEvent>(
            new EventCallback<CustomStyleResolvedEvent>(this.OnCustomStyleResolved));
        this.m_ScrollView.contentContainer.RegisterCallback<AttachToPanelEvent>(
            new EventCallback<AttachToPanelEvent>(this.OnAttachToPanel));
        this.m_ScrollView.contentContainer.RegisterCallback<DetachFromPanelEvent>(
            new EventCallback<DetachFromPanelEvent>(this.OnDetachFromPanel));
        this.hierarchy.Add((VisualElement)this.m_ScrollView);
        this.m_ScrollView.contentContainer.focusable = true;
        this.m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform;
        this.focusable = true;
        // TODO: Implement
        // this.isCompositeRoot = true;
        this.delegatesFocus = true;
        this.m_ItemIndexChangedCallback = new Action<int, int>(this.OnItemIndexChanged);
        this.m_ItemsSourceChangedCallback = new Action(this.OnItemsSourceChanged);
    }

    /// <summary>
    ///        <para>
    /// Constructs a BaseHorizontalCollectionView, with all required properties provided.
    /// </para>
    ///      </summary>
    /// <param name="itemsSource">The list of items to use as a data source.</param>
    /// <param name="itemWidth">The width of each item, in pixels. For &lt;c&gt;FixedWidth&lt;/c&gt; virtualization only.</param>
    public BaseHorizontalCollectionView(IList itemsSource, float itemWidth = -1f)
        : this()
    {
        if ((double)Math.Abs(itemWidth - -1f) > 1.401298464324817E-45)
        {
            this.m_FixedItemWidth = itemWidth;
            this.m_ItemWidthIsInline = true;
        }

        if (itemsSource == null)
            return;
        this.itemsSource = itemsSource;
    }

    [Obsolete(
        "makeItem and bindItem are now in ListView and TreeView directly, please use a constructor without these parameters.")]
    public BaseHorizontalCollectionView(
        IList itemsSource,
        float itemWidth = -1f,
        Func<VisualElement> makeItem = null,
        Action<VisualElement, int> bindItem = null)
        : this()
    {
        if ((double)Math.Abs(itemWidth - -1f) > 1.401298464324817E-45)
        {
            this.m_FixedItemWidth = itemWidth;
            this.m_ItemWidthIsInline = true;
        }

        this.itemsSource = itemsSource;
    }

    /// <summary>
    ///        <para>
    /// Gets the root element of the specified collection view item.
    /// </para>
    ///      </summary>
    /// <param name="id">The item identifier.</param>
    /// <returns>
    ///   <para>The item's root element.</para>
    /// </returns>
    public VisualElement GetRootElementForId(int id) => this.activeItems
        .FirstOrDefault<ReusableCollectionItem>((Func<ReusableCollectionItem, bool>)(t => t.id == id))?.rootElement;

    /// <summary>
    ///        <para>
    /// Gets the root element the specified collection view item.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    /// <returns>
    ///   <para>The item's root element.</para>
    /// </returns>
    public VisualElement GetRootElementForIndex(int index) =>
        this.GetRootElementForId(this.viewController.GetIdForIndex(index));

    internal virtual bool HasValidDataAndBindings() => this.m_ViewController != null && this.itemsSource != null;

    private void OnItemIndexChanged(int srcIndex, int dstIndex)
    {
        Action<int, int> itemIndexChanged = this.itemIndexChanged;
        if (itemIndexChanged != null)
            itemIndexChanged(srcIndex, dstIndex);
        this.RefreshItems();
    }

    private void OnItemsSourceChanged()
    {
        Action itemsSourceChanged = this.itemsSourceChanged;
        if (itemsSourceChanged == null)
            return;
        itemsSourceChanged();
    }

    /// <summary>
    ///        <para>
    /// Rebinds a single item if it is currently visible in the collection view.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    public void RefreshItem(int index)
    {
        foreach (ReusableCollectionItem activeItem in this.activeItems)
        {
            if (activeItem.index == index)
            {
                this.viewController.InvokeBindItem(activeItem, activeItem.index);
                break;
            }
        }
    }

    /// <summary>
    ///        <para>
    /// Rebinds all items currently visible.
    /// </para>
    ///      </summary>
    public void RefreshItems()
    {
        using (new ProfilerMarker("BaseHorizontalCollectionView.RefreshItems").Auto())
        {
            if (this.m_ViewController == null)
                return;
            this.RefreshSelection();
            this.virtualizationController.Refresh(false);
            this.PostRefresh();
        }
    }

    [Obsolete("Refresh() has been deprecated. Use Rebuild() instead. (UnityUpgradable) -> Rebuild()", false)]
    public void Refresh() => this.Rebuild();

    /// <summary>
    ///        <para>
    /// Clears the collection view, recreates all visible visual elements, and rebinds all items.
    /// </para>
    ///      </summary>
    public void Rebuild()
    {
        using (new ProfilerMarker("BaseHorizontalCollectionView.Rebuild").Auto())
        {
            if (this.m_ViewController == null)
                return;
            this.RefreshSelection();
            this.virtualizationController.Refresh(true);
            this.PostRefresh();
        }
    }

    private void RefreshSelection()
    {
        this.m_SelectedIndices.Clear();
        this.m_SelectedItems.Clear();
        if (this.viewController?.itemsSource == null || this.m_SelectedIds.Count <= 0)
            return;
        int count = this.viewController.itemsSource.Count;
        for (int index = 0; index < count; ++index)
        {
            if (this.m_SelectedIds.Contains(this.viewController.GetIdForIndex(index)))
            {
                this.m_SelectedIndices.Add(index);
                this.m_SelectedItems.Add(this.viewController.GetItemForIndex(index));
            }
        }
    }

    private protected virtual void PostRefresh()
    {
        if (!this.HasValidDataAndBindings())
            return;
        Rect layout = this.m_ScrollView.layout;
        this.m_LastWidth = layout.width;
        this.m_LastHeight = layout.height;
        layout = this.m_ScrollView.layout;
        if (float.IsNaN(layout.width))
            return;
        layout = this.m_ScrollView.layout;
        this.Resize(layout.size);
    }

    /// <summary>
    ///        <para>
    /// Scrolls to a specific VisualElement.
    /// </para>
    ///      </summary>
    /// <param name="visualElement">The element to scroll to.</param>
    public void ScrollTo(VisualElement visualElement) => this.m_ScrollView.ScrollTo(visualElement);

    /// <summary>
    ///        <para>
    /// Scrolls to a specific item index and makes it visible.
    /// </para>
    ///      </summary>
    /// <param name="index">Item index to scroll to. Specify -1 to make the last item visible.</param>
    public void ScrollToItem(int index)
    {
        if (!this.HasValidDataAndBindings())
            return;
        this.virtualizationController.ScrollToItem(index);
    }

    /// <summary>
    ///        <para>
    /// Scrolls to a specific item id and makes it visible.
    /// </para>
    ///      </summary>
    /// <param name="id">Item id to scroll to.</param>
    [Obsolete(
        "ScrollToId() has been deprecated. Use ScrollToItemById() instead. (UnityUpgradable) -> ScrollToItemById(*)",
        false)]
    public void ScrollToId(int id) => this.ScrollToItemById(id);

    /// <summary>
    ///        <para>
    /// Scrolls to a specific item id and makes it visible.
    /// </para>
    ///      </summary>
    /// <param name="id">Item id to scroll to.</param>
    public void ScrollToItemById(int id)
    {
        if (!this.HasValidDataAndBindings())
            return;
        this.virtualizationController.ScrollToItem(this.viewController.GetIndexForId(id));
    }

    private void OnScroll(Vector2 offset)
    {
        if (!this.HasValidDataAndBindings())
            return;
        this.virtualizationController.OnScroll(offset);
    }

    private void Resize(Vector2 size)
    {
        this.virtualizationController.Resize(size);
        this.m_LastWidth = size.x;
        this.m_LastHeight = size.y;
        this.virtualizationController.UpdateBackground();
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        if (evt.destinationPanel == null)
            return;
        this.m_ScrollView.contentContainer.AddManipulator((IManipulator)(this.m_NavigationManipulator =
            new KeyboardNavigationManipulator(new Action<KeyboardNavigationOperation, EventBase>(this.Apply))));
        this.m_ScrollView.contentContainer.RegisterCallback<PointerMoveEvent>(
            new EventCallback<PointerMoveEvent>(this.OnPointerMove));
        this.m_ScrollView.contentContainer.RegisterCallback<PointerDownEvent>(
            new EventCallback<PointerDownEvent>(this.OnPointerDown));
        this.m_ScrollView.contentContainer.RegisterCallback<PointerCancelEvent>(
            new EventCallback<PointerCancelEvent>(this.OnPointerCancel));
        this.m_ScrollView.contentContainer.RegisterCallback<PointerUpEvent>(
            new EventCallback<PointerUpEvent>(this.OnPointerUp));
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        if (evt.originPanel == null)
            return;
        this.m_ScrollView.contentContainer.RemoveManipulator((IManipulator)this.m_NavigationManipulator);
        this.m_ScrollView.contentContainer.UnregisterCallback<PointerMoveEvent>(
            new EventCallback<PointerMoveEvent>(this.OnPointerMove));
        this.m_ScrollView.contentContainer.UnregisterCallback<PointerDownEvent>(
            new EventCallback<PointerDownEvent>(this.OnPointerDown));
        this.m_ScrollView.contentContainer.UnregisterCallback<PointerCancelEvent>(
            new EventCallback<PointerCancelEvent>(this.OnPointerCancel));
        this.m_ScrollView.contentContainer.UnregisterCallback<PointerUpEvent>(
            new EventCallback<PointerUpEvent>(this.OnPointerUp));
    }

    // [Obsolete(
    //     "OnKeyDown is obsolete and will be removed from ListView. Use the event system instead, i.e. SendEvent(EventBase e).",
    //     true)]
    // public void OnKeyDown(KeyDownEvent evt) => this.m_NavigationManipulator.OnKeyDown(evt);

    private bool Apply(KeyboardNavigationOperation op, bool shiftKey)
    {
        if (this.selectionType == SelectionType.None || !this.HasValidDataAndBindings())
            return false;
        switch (op)
        {
            case KeyboardNavigationOperation.SelectAll:
                this.SelectAll();
                return true;
            case KeyboardNavigationOperation.Cancel:
                this.ClearSelection();
                return true;
            case KeyboardNavigationOperation.Submit:
                Action<IEnumerable<object>> itemsChosen = this.itemsChosen;
                if (itemsChosen != null)
                    itemsChosen((IEnumerable<object>)this.m_SelectedItems);
                this.ScrollToItem(this.selectedIndex);
                return true;
            case KeyboardNavigationOperation.Previous:
                if (this.selectedIndex > 0)
                {
                    HandleSelectionAndScroll(this.selectedIndex - 1);
                    return true;
                }

                break;
            case KeyboardNavigationOperation.Next:
                if (this.selectedIndex + 1 < this.m_ViewController.itemsSource.Count)
                {
                    HandleSelectionAndScroll(this.selectedIndex + 1);
                    return true;
                }

                break;
            case KeyboardNavigationOperation.PageUp:
                if (this.m_SelectedIndices.Count > 0)
                    HandleSelectionAndScroll(Mathf.Max(0,
                        (this.m_IsRangeSelectionDirectionUp
                            ? this.m_SelectedIndices.Min()
                            : this.m_SelectedIndices.Max()) - (this.virtualizationController.visibleItemCount - 1)));
                return true;
            case KeyboardNavigationOperation.PageDown:
                if (this.m_SelectedIndices.Count > 0)
                    HandleSelectionAndScroll(Mathf.Min(this.viewController.itemsSource.Count - 1,
                        (this.m_IsRangeSelectionDirectionUp
                            ? this.m_SelectedIndices.Min()
                            : this.m_SelectedIndices.Max()) + (this.virtualizationController.visibleItemCount - 1)));
                return true;
            case KeyboardNavigationOperation.Begin:
                HandleSelectionAndScroll(0);
                return true;
            case KeyboardNavigationOperation.End:
                HandleSelectionAndScroll(this.m_ViewController.itemsSource.Count - 1);
                return true;
        }

        return false;

        void HandleSelectionAndScroll(int index)
        {
            if (index < 0 || index >= this.m_ViewController.itemsSource.Count)
                return;
            if (this.selectionType == SelectionType.Multiple & shiftKey && this.m_SelectedIndices.Count != 0)
                this.DoRangeSelection(index);
            else
                this.selectedIndex = index;
            this.ScrollToItem(index);
        }
    }

    private void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
    {
        int num;
        switch (sourceEvent)
        {
            case KeyDownEvent keyDownEvent when keyDownEvent.shiftKey:
                num = 1;
                break;
            case INavigationEvent navigationEvent:
                num = navigationEvent.shiftKey ? 1 : 0;
                break;
            default:
                num = 0;
                break;
        }

        bool shiftKey = num != 0;
        if (!this.Apply(op, shiftKey))
            return;
        sourceEvent.StopPropagation();
        sourceEvent.PreventDefault();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.button != 0)
            return;
        if ((evt.pressedButtons & 1) == 0)
            this.ProcessPointerUp((IPointerEvent)evt);
        else
            this.ProcessPointerDown((IPointerEvent)evt);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.pointerType != PointerType.mouse)
        {
            this.ProcessPointerDown((IPointerEvent)evt);
            // TODO: Implement
            // this.panel.PreventCompatibilityMouseEvents(evt.pointerId);
        }
        else
            this.ProcessPointerDown((IPointerEvent)evt);
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        if (!this.HasValidDataAndBindings() || !evt.isPrimary)
            return;
        this.ClearSelection();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerType != PointerType.mouse)
        {
            this.ProcessPointerUp((IPointerEvent)evt);
            // TODO: Implement
            // this.panel.PreventCompatibilityMouseEvents(evt.pointerId);
        }
        else
            this.ProcessPointerUp((IPointerEvent)evt);
    }

    private void ProcessPointerDown(IPointerEvent evt)
    {
        if (!this.HasValidDataAndBindings() || !evt.isPrimary || evt.button != 0)
            return;
        if (evt.pointerType != PointerType.mouse)
            this.m_TouchDownPosition = evt.position;
        else
            this.DoSelect((Vector2)evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
    }

    private void ProcessPointerUp(IPointerEvent evt)
    {
        if (!this.HasValidDataAndBindings() || !evt.isPrimary || evt.button != 0)
            return;
        if (evt.pointerType != PointerType.mouse)
        {
            if ((double)(evt.position - this.m_TouchDownPosition).sqrMagnitude > 100.0)
                return;
            this.DoSelect((Vector2)evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
        }
        else
        {
            int indexFromPosition = this.virtualizationController.GetIndexFromPosition((Vector2)evt.localPosition);
            if (this.selectionType == SelectionType.Multiple && !evt.shiftKey && !evt.actionKey &&
                this.m_SelectedIndices.Count > 1 && this.m_SelectedIndices.Contains(indexFromPosition))
                this.ProcessSingleClick(indexFromPosition);
        }
    }

    private void DoSelect(Vector2 localPosition, int clickCount, bool actionKey, bool shiftKey)
    {
        int indexFromPosition = this.virtualizationController.GetIndexFromPosition(localPosition);
        if (indexFromPosition > this.viewController.itemsSource.Count - 1 || this.selectionType == SelectionType.None)
            return;
        int idForIndex = this.viewController.GetIdForIndex(indexFromPosition);
        switch (clickCount)
        {
            case 1:
                if (this.selectionType == SelectionType.Multiple & actionKey)
                {
                    if (this.m_SelectedIds.Contains(idForIndex))
                    {
                        this.RemoveFromSelection(indexFromPosition);
                        break;
                    }

                    this.AddToSelection(indexFromPosition);
                    break;
                }

                if (this.selectionType == SelectionType.Multiple & shiftKey)
                {
                    if (this.m_SelectedIndices.Count == 0)
                    {
                        this.SetSelection(indexFromPosition);
                        break;
                    }

                    this.DoRangeSelection(indexFromPosition);
                    break;
                }

                if (this.selectionType == SelectionType.Multiple && this.m_SelectedIndices.Contains(indexFromPosition))
                {
                    Action selectionNotChanged = this.selectionNotChanged;
                    if (selectionNotChanged == null)
                        break;
                    selectionNotChanged();
                    break;
                }

                if (this.selectionType == SelectionType.Single && this.m_SelectedIndices.Contains(indexFromPosition))
                {
                    Action selectionNotChanged = this.selectionNotChanged;
                    if (selectionNotChanged != null)
                        selectionNotChanged();
                }

                this.SetSelection(indexFromPosition);
                break;
            case 2:
                if (this.itemsChosen != null)
                    this.ProcessSingleClick(indexFromPosition);
                Action<IEnumerable<object>> itemsChosen = this.itemsChosen;
                if (itemsChosen == null)
                    break;
                itemsChosen((IEnumerable<object>)this.m_SelectedItems);
                break;
        }
    }

    internal void DoRangeSelection(int rangeSelectionFinalIndex)
    {
        int num = this.m_IsRangeSelectionDirectionUp ? this.m_SelectedIndices.Max() : this.m_SelectedIndices.Min();
        this.ClearSelectionWithoutValidation();
        List<int> indexes = new List<int>();
        this.m_IsRangeSelectionDirectionUp = rangeSelectionFinalIndex < num;
        if (this.m_IsRangeSelectionDirectionUp)
        {
            for (int index = rangeSelectionFinalIndex; index <= num; ++index)
                indexes.Add(index);
        }
        else
        {
            for (int index = rangeSelectionFinalIndex; index >= num; --index)
                indexes.Add(index);
        }

        this.AddToSelection((IList<int>)indexes);
    }

    private void ProcessSingleClick(int clickedIndex) => this.SetSelection(clickedIndex);

    internal void SelectAll()
    {
        if (!this.HasValidDataAndBindings() || this.selectionType != SelectionType.Multiple)
            return;
        for (int index = 0; index < this.m_ViewController.itemsSource.Count; ++index)
        {
            int idForIndex = this.viewController.GetIdForIndex(index);
            object itemForIndex = this.viewController.GetItemForIndex(index);
            foreach (ReusableCollectionItem activeItem in this.activeItems)
            {
                if (activeItem.id == idForIndex)
                    activeItem.SetSelected(true);
            }

            if (!this.m_SelectedIds.Contains(idForIndex))
            {
                this.m_SelectedIds.Add(idForIndex);
                this.m_SelectedIndices.Add(index);
                this.m_SelectedItems.Add(itemForIndex);
            }
        }

        this.NotifyOfSelectionChange();
        // TODO: Implement
        // this.SaveViewData();
    }

    /// <summary>
    ///        <para>
    /// Adds an item to the collection of selected items.
    /// </para>
    ///      </summary>
    /// <param name="index">Item index.</param>
    public void AddToSelection(int index) => this.AddToSelection((IList<int>)new int[1] { index });

    internal void AddToSelection(IList<int> indexes)
    {
        if (!this.HasValidDataAndBindings() || indexes == null || indexes.Count == 0)
            return;
        foreach (int index in (IEnumerable<int>)indexes)
            this.AddToSelectionWithoutValidation(index);
        this.NotifyOfSelectionChange();

        // TODO: Implement
        // this.SaveViewData();
    }

    private void AddToSelectionWithoutValidation(int index)
    {
        if (this.m_SelectedIndices.Contains(index))
            return;
        int idForIndex = this.viewController.GetIdForIndex(index);
        object itemForIndex = this.viewController.GetItemForIndex(index);
        foreach (ReusableCollectionItem activeItem in this.activeItems)
        {
            if (activeItem.id == idForIndex)
                activeItem.SetSelected(true);
        }

        this.m_SelectedIds.Add(idForIndex);
        this.m_SelectedIndices.Add(index);
        this.m_SelectedItems.Add(itemForIndex);
    }

    /// <summary>
    ///        <para>
    /// Removes an item from the collection of selected items.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    public void RemoveFromSelection(int index)
    {
        if (!this.HasValidDataAndBindings())
            return;
        this.RemoveFromSelectionWithoutValidation(index);
        this.NotifyOfSelectionChange();

        // TODO: Implement   
        // this.SaveViewData();
    }

    private void RemoveFromSelectionWithoutValidation(int index)
    {
        if (!this.m_SelectedIndices.Contains(index))
            return;
        int idForIndex = this.viewController.GetIdForIndex(index);
        object itemForIndex = this.viewController.GetItemForIndex(index);
        foreach (ReusableCollectionItem activeItem in this.activeItems)
        {
            if (activeItem.id == idForIndex)
                activeItem.SetSelected(false);
        }

        this.m_SelectedIds.Remove(idForIndex);
        this.m_SelectedIndices.Remove(index);
        this.m_SelectedItems.Remove(itemForIndex);
    }

    /// <summary>
    ///        <para>
    /// Sets the currently selected item.
    /// </para>
    ///      </summary>
    /// <param name="index">The item index.</param>
    public void SetSelection(int index)
    {
        if (index < 0)
            this.ClearSelection();
        else
            this.SetSelection((IEnumerable<int>)new int[1] { index });
    }

    public void SetSelection(IEnumerable<int> indices) => this.SetSelectionInternal(indices, true);

    public void SetSelectionWithoutNotify(IEnumerable<int> indices) => this.SetSelectionInternal(indices, false);

    internal void SetSelectionInternal(IEnumerable<int> indices, bool sendNotification)
    {
        if (!this.HasValidDataAndBindings() || indices == null || this.MatchesExistingSelection(indices))
            return;
        this.ClearSelectionWithoutValidation();
        foreach (int index in indices)
            this.AddToSelectionWithoutValidation(index);
        if (sendNotification)
            this.NotifyOfSelectionChange();

        // TODO: Implement
        // this.SaveViewData();
    }

    private bool MatchesExistingSelection(IEnumerable<int> indices)
    {
        List<int> toRelease = CollectionPool<List<int>, int>.Get();
        try
        {
            toRelease.AddRange(indices);
            if (toRelease.Count != this.m_SelectedIndices.Count)
                return false;
            for (int index = 0; index < toRelease.Count; ++index)
            {
                if (toRelease[index] != this.m_SelectedIndices[index])
                    return false;
            }

            return true;
        }
        finally
        {
            CollectionPool<List<int>, int>.Release(toRelease);
        }
    }

    private void NotifyOfSelectionChange()
    {
        if (!this.HasValidDataAndBindings())
            return;
        Action<IEnumerable<object>> selectionChanged = this.selectionChanged;
        if (selectionChanged != null)
            selectionChanged((IEnumerable<object>)this.m_SelectedItems);
        Action<IEnumerable<int>> selectedIndicesChanged = this.selectedIndicesChanged;
        if (selectedIndicesChanged == null)
            return;
        selectedIndicesChanged((IEnumerable<int>)this.m_SelectedIndices);
    }

    /// <summary>
    ///        <para>
    /// Deselects any selected items.
    /// </para>
    ///      </summary>
    public void ClearSelection()
    {
        if (!this.HasValidDataAndBindings() || this.m_SelectedIds.Count == 0)
            return;
        this.ClearSelectionWithoutValidation();
        this.NotifyOfSelectionChange();
    }

    private void ClearSelectionWithoutValidation()
    {
        foreach (ReusableCollectionItem activeItem in this.activeItems)
            activeItem.SetSelected(false);
        this.m_SelectedIds.Clear();
        this.m_SelectedIndices.Clear();
        this.m_SelectedItems.Clear();
    }

    // TODO: Implement
    // internal override void OnViewDataReady()
    // {
    //     base.OnViewDataReady();
    //     this.OverwriteFromViewData((object)this, this.GetFullHierarchicalViewDataKey());
    // }

    [EventInterest(new System.Type[]
    {
        typeof(PointerUpEvent), typeof(FocusEvent), typeof(NavigationSubmitEvent), typeof(BlurEvent)
    })]
    protected override void ExecuteDefaultAction(EventBase evt)
    {
        base.ExecuteDefaultAction(evt);
        if (evt.eventTypeId == EventBase<PointerUpEvent>.TypeId())
            this.mHDragger?.OnPointerUpEvent((PointerUpEvent)evt);

        // TODO: Implement
        // else if (evt.eventTypeId == EventBase<FocusEvent>.TypeId())
        // this.m_VirtualizationController?.OnFocus(evt.leafTarget as VisualElement);
        else if (evt.eventTypeId == EventBase<BlurEvent>.TypeId())
        {
            this.m_VirtualizationController?.OnBlur((evt as BlurEvent)?.relatedTarget as VisualElement);
        }
        else
        {
            if (evt.eventTypeId != EventBase<NavigationSubmitEvent>.TypeId() || evt.target != this)
                return;
            this.m_ScrollView.contentContainer.Focus();
        }
    }

    private void OnSizeChanged(GeometryChangedEvent evt)
    {
        if (!this.HasValidDataAndBindings())
            return;
        double height1 = (double)evt.newRect.height;
        Rect rect = evt.oldRect;
        double height2 = (double)rect.height;
        int num;
        if (Mathf.Approximately((float)height1, (float)height2))
        {
            rect = evt.newRect;
            double width1 = (double)rect.width;
            rect = evt.oldRect;
            double width2 = (double)rect.width;
            num = Mathf.Approximately((float)width1, (float)width2) ? 1 : 0;
        }
        else
            num = 0;

        if (num != 0)
            return;
        rect = evt.newRect;
        
        if (Mathf.Abs(rect.size.x - m_LastWidth) > 0.01f
            // Only check for horizontal size change
            // || Mathf.Abs(rect.size.y - m_LastHeight) > 0.01f
            )
        {
            this.Resize(rect.size);
        }
    }

    private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        int num;
        if (this.m_ItemWidthIsInline ||
            !e.customStyle.TryGetValue(BaseHorizontalCollectionView.s_ItemWidthProperty, out num) ||
            (double)Math.Abs(this.m_FixedItemWidth - (float)num) <= 1.401298464324817E-45)
            return;
        this.m_FixedItemWidth = (float)num;
        this.RefreshItems();
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() => this.RefreshItems();

    /// <summary>
    ///        <para>
    /// Defines UxmlTraits for the BaseHorizontalCollectionView.
    /// </para>
    ///      </summary>
    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        private readonly UxmlIntAttributeDescription m_FixedItemWidth;
        private readonly UxmlEnumAttributeDescription<CollectionVirtualizationMethod> m_VirtualizationMethod;
        private readonly UxmlBoolAttributeDescription m_ShowBorder;
        private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType;
        private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds;
        private readonly UxmlBoolAttributeDescription m_Reorderable;
        private readonly UxmlBoolAttributeDescription m_HorizontalScrollingEnabled;

        /// <summary>
        ///        <para>
        /// Returns an empty enumerable, because list views usually do not have child elements.
        /// </para>
        ///      </summary>
        /// <returns>
        ///   <para>An empty enumerable.</para>
        /// </returns>
        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get
            {
                yield break;
            }
        }

        /// <summary>
        ///        <para>
        /// Initializes BaseHorizontalCollectionView properties using values from the attribute bag.
        /// </para>
        ///      </summary>
        /// <param name="ve">The object to initialize.</param>
        /// <param name="bag">The attribute bag.</param>
        /// <param name="cc">The creation context; unused.</param>
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            int num = 0;
            BaseHorizontalCollectionView horizontalCollectionView = (BaseHorizontalCollectionView)ve;
            horizontalCollectionView.reorderable = this.m_Reorderable.GetValueFromBag(bag, cc);
            if (this.m_FixedItemWidth.TryGetValueFromBag(bag, cc, ref num))
                horizontalCollectionView.fixedItemWidth = (float)num;
            horizontalCollectionView.virtualizationMethod = this.m_VirtualizationMethod.GetValueFromBag(bag, cc);
            horizontalCollectionView.showBorder = this.m_ShowBorder.GetValueFromBag(bag, cc);
            horizontalCollectionView.selectionType = this.m_SelectionType.GetValueFromBag(bag, cc);
            horizontalCollectionView.showAlternatingRowBackgrounds =
                this.m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
            horizontalCollectionView.verticalScrollingEnabled =
                this.m_HorizontalScrollingEnabled.GetValueFromBag(bag, cc);
        }

        public UxmlTraits()
        {
            UxmlIntAttributeDescription attributeDescription1 = new UxmlIntAttributeDescription();
            attributeDescription1.name = "fixed-item-width";
            attributeDescription1.obsoleteNames = (IEnumerable<string>)new string[1] { "itemWidth, item-width" };
            attributeDescription1.defaultValue = BaseHorizontalCollectionView.s_DefaultItemWidth;
            this.m_FixedItemWidth = attributeDescription1;
            UxmlEnumAttributeDescription<CollectionVirtualizationMethod> attributeDescription2 =
                new UxmlEnumAttributeDescription<CollectionVirtualizationMethod>();
            attributeDescription2.name = "virtualization-method";
            attributeDescription2.defaultValue = CollectionVirtualizationMethod.Fixed;
            this.m_VirtualizationMethod = attributeDescription2;
            UxmlBoolAttributeDescription attributeDescription3 = new UxmlBoolAttributeDescription();
            attributeDescription3.name = "show-border";
            attributeDescription3.defaultValue = false;
            this.m_ShowBorder = attributeDescription3;
            UxmlEnumAttributeDescription<SelectionType> attributeDescription4 =
                new UxmlEnumAttributeDescription<SelectionType>();
            attributeDescription4.name = "selection-type";
            attributeDescription4.defaultValue = SelectionType.Single;
            this.m_SelectionType = attributeDescription4;
            UxmlEnumAttributeDescription<AlternatingRowBackground> attributeDescription5 =
                new UxmlEnumAttributeDescription<AlternatingRowBackground>();
            attributeDescription5.name = "show-alternating-row-backgrounds";
            attributeDescription5.defaultValue = AlternatingRowBackground.None;
            this.m_ShowAlternatingRowBackgrounds = attributeDescription5;
            UxmlBoolAttributeDescription attributeDescription6 = new UxmlBoolAttributeDescription();
            attributeDescription6.name = "reorderable";
            attributeDescription6.defaultValue = false;
            this.m_Reorderable = attributeDescription6;
            UxmlBoolAttributeDescription attributeDescription7 = new UxmlBoolAttributeDescription();
            attributeDescription7.name = "horizontal-scrolling";
            attributeDescription7.defaultValue = false;
            this.m_HorizontalScrollingEnabled = attributeDescription7;

            // ISSUE: explicit constructor call
            // base.\u002Ector();
        }
    }
}
