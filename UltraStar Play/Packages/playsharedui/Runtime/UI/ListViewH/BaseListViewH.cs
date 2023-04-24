// Decompiled with JetBrains decompiler
// Type: UnityEngine.UIElements.BaseListView
// Unity 2022.2.4f1

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

/// <summary>
///        <para>
/// Base class for a list view, a horizontally scrollable area that links to, and displays, a list of items.
/// </para>
///      </summary>
public abstract class BaseListViewH : BaseHorizontalCollectionView
{
    private bool m_ShowBoundCollectionSize = true;
    private bool m_ShowFoldoutHeader;
    private string m_HeaderTitle;
    private Label m_ListViewLabel;
    private Foldout m_Foldout;
    private TextField m_ArraySizeField;
    private bool m_IsOverMultiEditLimit;
    private int m_MaxMultiEditCount;
    private VisualElement m_Footer;
    private Button m_AddButton;
    private Button m_RemoveButton;
    private Action<IEnumerable<int>> m_ItemAddedCallback;
    private Action<IEnumerable<int>> m_ItemRemovedCallback;
    private Action m_ItemsSourceSizeChangedCallback;
    private ListViewReorderMode m_ReorderMode;

    /// <summary>
    ///        <para>
    /// The USS class name for ListView elements.
    /// </para>
    ///      </summary>
    public new static readonly string ussClassName = "unity-list-view";

    /// <summary>
    ///        <para>
    /// The USS class name of item elements in ListView elements.
    /// </para>
    ///      </summary>
    public new static readonly string itemUssClassName = BaseListView.ussClassName + "__item";

    /// <summary>
    ///        <para>
    /// The USS class name for label displayed when ListView is empty.
    /// </para>
    ///      </summary>
    public static readonly string emptyLabelUssClassName = BaseListView.ussClassName + "__empty-label";

    /// <summary>
    ///        <para>
    ///  The USS class name for label displayed when ListView is trying to edit too many items.
    /// </para>
    ///      </summary>
    public static readonly string overMaxMultiEditLimitClassName =
        BaseListView.ussClassName + "__over-max-multi-edit-limit-label";

    /// <summary>
    ///        <para>
    /// The USS class name for reorderable animated ListView elements.
    /// </para>
    ///      </summary>
    public static readonly string reorderableUssClassName = BaseListView.ussClassName + "__reorderable";

    /// <summary>
    ///        <para>
    /// The USS class name for item elements in reorderable animated ListView.
    /// </para>
    ///      </summary>
    public static readonly string reorderableItemUssClassName = BaseListView.reorderableUssClassName + "-item";

    /// <summary>
    ///        <para>
    /// The USS class name for item container in reorderable animated ListView.
    /// </para>
    ///      </summary>
    public static readonly string reorderableItemContainerUssClassName =
        BaseListView.reorderableItemUssClassName + "__container";

    /// <summary>
    ///        <para>
    /// The USS class name for drag handle in reorderable animated ListView.
    /// </para>
    ///      </summary>
    public static readonly string reorderableItemHandleUssClassName = BaseListView.reorderableUssClassName + "-handle";

    /// <summary>
    ///        <para>
    /// The USS class name for drag handle bar in reorderable animated ListView.
    /// </para>
    ///      </summary>
    public static readonly string reorderableItemHandleBarUssClassName =
        BaseListView.reorderableItemHandleUssClassName + "-bar";

    /// <summary>
    ///        <para>
    /// The USS class name for the footer of the ListView.
    /// </para>
    ///      </summary>
    public static readonly string footerUssClassName = BaseListView.ussClassName + "__footer";

    /// <summary>
    ///        <para>
    /// The USS class name for the foldout header of the ListView.
    /// </para>
    ///      </summary>
    public static readonly string foldoutHeaderUssClassName = BaseListView.ussClassName + "__foldout-header";

    /// <summary>
    ///        <para>
    /// The USS class name for the size field of the ListView when foldout header is enabled.
    /// </para>
    ///      </summary>
    public static readonly string arraySizeFieldUssClassName = BaseListView.ussClassName + "__size-field";

    /// <summary>
    ///        <para>
    /// The USS class name for ListView when foldout header is enabled.
    /// </para>
    ///      </summary>
    public static readonly string listViewWithHeaderUssClassName = BaseListView.ussClassName + "--with-header";

    /// <summary>
    ///        <para>
    /// The USS class name for ListView when add/remove footer is enabled.
    /// </para>
    ///      </summary>
    public static readonly string listViewWithFooterUssClassName = BaseListView.ussClassName + "--with-footer";

    /// <summary>
    ///        <para>
    /// The USS class name for scroll view when add/remove footer is enabled.
    /// </para>
    ///      </summary>
    public static readonly string scrollViewWithFooterUssClassName =
        BaseListView.ussClassName + "__scroll-view--with-footer";

    internal static readonly string footerAddButtonName = BaseListView.ussClassName + "__add-button";
    internal static readonly string footerRemoveButtonName = BaseListView.ussClassName + "__remove-button";
    private string m_MaxMultiEditStr;
    private static readonly string k_EmptyListStr = "List is empty";

    /// <summary>
    ///        <para>
    /// This property controls whether the list view displays the collection size (number of items).
    /// </para>
    ///      </summary>
    public bool showBoundCollectionSize
    {
        get => this.m_ShowBoundCollectionSize;
        set
        {
            if (this.m_ShowBoundCollectionSize == value)
                return;
            this.m_ShowBoundCollectionSize = value;
            this.SetupArraySizeField();
        }
    }

    internal override bool sourceIncludesArraySize =>
        this.showBoundCollectionSize && this.binding != null && !this.showFoldoutHeader;

    /// <summary>
    ///        <para>
    /// This property controls whether the list view displays a header, in the form of a foldout that can be expanded or collapsed.
    /// </para>
    ///      </summary>
    public bool showFoldoutHeader
    {
        get => this.m_ShowFoldoutHeader;
        set
        {
            if (this.m_ShowFoldoutHeader == value)
                return;
            this.m_ShowFoldoutHeader = value;
            this.EnableInClassList(BaseListView.listViewWithHeaderUssClassName, value);
            if (this.m_ShowFoldoutHeader)
            {
                if (this.m_Foldout != null)
                    return;
                Foldout foldout = new Foldout();
                foldout.name = BaseListView.foldoutHeaderUssClassName;
                foldout.text = this.m_HeaderTitle;
                this.m_Foldout = foldout;
                this.m_Foldout.AddToClassList(BaseListView.foldoutHeaderUssClassName);
                this.m_Foldout.tabIndex = 1;
                this.hierarchy.Add((VisualElement)this.m_Foldout);
                this.m_Foldout.Add((VisualElement)this.scrollView);
            }
            else if (this.m_Foldout != null)
            {
                this.m_Foldout?.RemoveFromHierarchy();
                this.m_Foldout = (Foldout)null;
                this.hierarchy.Add((VisualElement)this.scrollView);
            }

            this.SetupArraySizeField();
            this.UpdateListViewLabel();
            if (!this.showAddRemoveFooter)
                return;
            this.EnableFooter(true);
        }
    }

    private void SetupArraySizeField()
    {
        if (this.sourceIncludesArraySize || !this.showFoldoutHeader || !this.showBoundCollectionSize)
        {
            this.m_ArraySizeField?.RemoveFromHierarchy();
            this.m_ArraySizeField = (TextField)null;
        }
        else
        {
            TextField textField = new TextField();
            textField.name = BaseListView.arraySizeFieldUssClassName;
            this.m_ArraySizeField = textField;
            this.m_ArraySizeField.AddToClassList(BaseListView.arraySizeFieldUssClassName);
            this.m_ArraySizeField.RegisterValueChangedCallback<string>(
                new EventCallback<ChangeEvent<string>>(this.OnArraySizeFieldChanged));
            this.m_ArraySizeField.isDelayed = true;
            this.m_ArraySizeField.focusable = true;
            this.hierarchy.Add((VisualElement)this.m_ArraySizeField);
            this.UpdateArraySizeField();
        }
    }

    /// <summary>
    ///        <para>
    /// This property controls the text of the foldout header when using showFoldoutHeader.
    /// </para>
    ///      </summary>
    public string headerTitle
    {
        get => this.m_HeaderTitle;
        set
        {
            this.m_HeaderTitle = value;
            if (this.m_Foldout == null)
                return;
            this.m_Foldout.text = this.m_HeaderTitle;
        }
    }

    /// <summary>
    ///        <para>
    /// This property controls whether a footer will be added to the list view.
    /// </para>
    ///      </summary>
    public bool showAddRemoveFooter
    {
        get => this.m_Footer != null;
        set => this.EnableFooter(value);
    }

    internal Foldout headerFoldout => this.m_Foldout;

    private void EnableFooter(bool enabled)
    {
        this.EnableInClassList(BaseListView.listViewWithFooterUssClassName, enabled);
        this.scrollView.EnableInClassList(BaseListView.scrollViewWithFooterUssClassName, enabled);
        if (enabled)
        {
            if (this.m_Footer == null)
            {
                this.m_Footer = new VisualElement() { name = BaseListView.footerUssClassName };
                this.m_Footer.AddToClassList(BaseListView.footerUssClassName);
                Button button1 = new Button(new Action(this.OnRemoveClicked));
                button1.name = BaseListViewH.footerRemoveButtonName;
                button1.text = "-";
                this.m_RemoveButton = button1;
                this.m_Footer.Add((VisualElement)this.m_RemoveButton);
                Button button2 = new Button(new Action(this.OnAddClicked));
                button2.name = BaseListViewH.footerAddButtonName;
                button2.text = "+";
                this.m_AddButton = button2;
                this.m_Footer.Add((VisualElement)this.m_AddButton);
            }

            if (this.m_Foldout != null)
                this.m_Foldout.contentContainer.Add(this.m_Footer);
            else
                this.hierarchy.Add(this.m_Footer);
        }
        else
        {
            this.m_RemoveButton?.RemoveFromHierarchy();
            this.m_AddButton?.RemoveFromHierarchy();
            this.m_Footer?.RemoveFromHierarchy();
            this.m_RemoveButton = (Button)null;
            this.m_AddButton = (Button)null;
            this.m_Footer = (VisualElement)null;
        }
    }

    public event Action<IEnumerable<int>> itemsAdded;

    public event Action<IEnumerable<int>> itemsRemoved;

    private void AddItems(int itemCount) => this.ViewHController.AddItems(itemCount);

    private void RemoveItems(List<int> indices) => this.ViewHController.RemoveItems(indices);

    private void OnArraySizeFieldChanged(ChangeEvent<string> evt)
    {
        if (this.m_ArraySizeField.showMixedValue && Constants.mixedValueString == evt.newValue)
            return;
        int result;
        if (!int.TryParse(evt.newValue, out result) || result < 0)
        {
            this.m_ArraySizeField.SetValueWithoutNotify(evt.previousValue);
        }
        else
        {
            int itemsCount = this.ViewHController.GetItemsCount();
            if (itemsCount == 0 && result == this.ViewHController.GetItemsMinCount())
                return;
            if (result > itemsCount)
                this.ViewHController.AddItems(result - itemsCount);
            else if (result < itemsCount)
                this.ViewHController.RemoveItems(itemsCount - result);
            else if (result == 0)
            {
                this.ViewHController.ClearItems();
                this.m_IsOverMultiEditLimit = false;
            }

            this.UpdateListViewLabel();
        }
    }

    internal void UpdateArraySizeField()
    {
        if (!this.HasValidDataAndBindings() || this.m_ArraySizeField == null)
            return;
        if (!this.m_ArraySizeField.showMixedValue)
            this.m_ArraySizeField.SetValueWithoutNotify(this.ViewHController.GetItemsMinCount().ToString());
        this.footer?.SetEnabled(!this.m_IsOverMultiEditLimit);
    }

    internal void UpdateListViewLabel()
    {
        if (!this.HasValidDataAndBindings())
            return;
        bool enable = this.itemsSource.Count == 0 && !this.sourceIncludesArraySize;
        if (this.m_IsOverMultiEditLimit)
        {
            if (this.m_ListViewLabel == null)
                this.m_ListViewLabel = new Label();
            this.m_ListViewLabel.text = this.m_MaxMultiEditStr;
            this.scrollView.contentViewport.Add((VisualElement)this.m_ListViewLabel);
        }
        else if (enable)
        {
            if (this.m_ListViewLabel == null)
                this.m_ListViewLabel = new Label();
            this.m_ListViewLabel.text = BaseListViewH.k_EmptyListStr;
            this.scrollView.contentViewport.Add((VisualElement)this.m_ListViewLabel);
        }
        else
        {
            this.m_ListViewLabel?.RemoveFromHierarchy();
            this.m_ListViewLabel = (Label)null;
        }

        this.m_ListViewLabel?.EnableInClassList(BaseListView.emptyLabelUssClassName, enable);
        this.m_ListViewLabel?.EnableInClassList(BaseListView.overMaxMultiEditLimitClassName,
            this.m_IsOverMultiEditLimit);
    }

    private void OnAddClicked()
    {
        this.AddItems(1);
        if (this.binding == null)
        {
            this.SetSelection(this.itemsSource.Count - 1);
            this.ScrollToItem(-1);
        }
        else
            this.schedule.Execute((Action)(() =>
            {
                this.SetSelection(this.itemsSource.Count - 1);
                this.ScrollToItem(-1);
            })).ExecuteLater(100L);

        if (!this.HasValidDataAndBindings() || this.m_ArraySizeField == null)
            return;
        this.m_ArraySizeField.showMixedValue = false;
    }

    private void OnRemoveClicked()
    {
        if (this.selectedIndices.Any<int>())
        {
            this.ViewHController.RemoveItems(this.selectedIndices.ToList<int>());
            this.ClearSelection();
        }
        else if (this.itemsSource.Count > 0)
            this.ViewHController.RemoveItem(this.itemsSource.Count - 1);

        if (!this.HasValidDataAndBindings() || this.m_ArraySizeField == null)
            return;
        this.m_ArraySizeField.showMixedValue = false;
    }

    internal TextField arraySizeField => this.m_ArraySizeField;

    internal void SetOverMaxMultiEditLimit(bool isOverLimit, int maxMultiEditCount)
    {
        this.m_IsOverMultiEditLimit = isOverLimit;
        this.m_MaxMultiEditCount = maxMultiEditCount;
        this.m_MaxMultiEditStr =
            string.Format(
                "This field cannot display arrays with more than {0} elements when multiple objects are selected.",
                (object)this.m_MaxMultiEditCount);
    }

    internal VisualElement footer => this.m_Footer;

    /// <summary>
    ///        <para>
    /// The view controller for this view, cast as a BaseListViewController.
    /// </para>
    ///      </summary>
    public BaseListViewHController ViewHController => base.viewController as BaseListViewHController;

    private protected override void CreateVirtualizationController() =>
        this.CreateVirtualizationController<ReusableListViewItem>();

    /// <summary>
    ///        <para>
    /// Assigns the view controller for this view and registers all events required for it to function properly.
    /// </para>
    ///      </summary>
    /// <param name="controller">The controller to use with this view.</param>
    public override void SetViewController(CollectionViewController controller)
    {
        if (this.m_ItemAddedCallback == null)
            this.m_ItemAddedCallback = new Action<IEnumerable<int>>(this.OnItemAdded);
        if (this.m_ItemRemovedCallback == null)
            this.m_ItemRemovedCallback = new Action<IEnumerable<int>>(this.OnItemsRemoved);
        if (this.m_ItemsSourceSizeChangedCallback == null)
            this.m_ItemsSourceSizeChangedCallback = new Action(this.OnItemsSourceSizeChanged);
        if (this.ViewHController != null)
        {
            this.ViewHController.itemsAdded -= this.m_ItemAddedCallback;
            this.ViewHController.itemsRemoved -= this.m_ItemRemovedCallback;
            this.ViewHController.itemsSourceSizeChanged -= this.m_ItemsSourceSizeChangedCallback;
        }

        base.SetViewController(controller);
        if (this.ViewHController == null)
            return;
        this.ViewHController.itemsAdded += this.m_ItemAddedCallback;
        this.ViewHController.itemsRemoved += this.m_ItemRemovedCallback;
        this.ViewHController.itemsSourceSizeChanged += this.m_ItemsSourceSizeChangedCallback;
    }

    private void OnItemAdded(IEnumerable<int> indices)
    {
        Action<IEnumerable<int>> itemsAdded = this.itemsAdded;
        if (itemsAdded == null)
            return;
        itemsAdded(indices);
    }

    private void OnItemsRemoved(IEnumerable<int> indices)
    {
        Action<IEnumerable<int>> itemsRemoved = this.itemsRemoved;
        if (itemsRemoved == null)
            return;
        itemsRemoved(indices);
    }

    private void OnItemsSourceSizeChanged()
    {
        if (this.binding is IInternalListViewBinding)
            return;
        this.RefreshItems();
    }

    internal event Action reorderModeChanged;

    /// <summary>
    ///        <para>
    /// This property controls the drag and drop mode for the list view.
    /// </para>
    ///      </summary>
    public ListViewReorderMode reorderMode
    {
        get => this.m_ReorderMode;
        set
        {
            if (value == this.m_ReorderMode)
                return;
            this.m_ReorderMode = value;
            this.InitializeDragAndDropController(this.reorderable);
            Action reorderModeChanged = this.reorderModeChanged;
            if (reorderModeChanged != null)
                reorderModeChanged();
            this.Rebuild();
        }
    }

    internal override ListViewHDragger CreateDragger() => this.m_ReorderMode == ListViewReorderMode.Simple
        ? new ListViewHDragger((BaseHorizontalCollectionView)this)
        : throw new NotImplementedException(
            "ListViewDraggerAnimated not implemented"); // (ListViewDragger) new ListViewDraggerAnimated((BaseHorizontalCollectionView) this);

    // TODO: Implement
    internal override ICollectionDragAndDropController CreateDragAndDropController() =>
        (ICollectionDragAndDropController)new DummyCollectionDragAndDropController();
    // internal override ICollectionDragAndDropController CreateDragAndDropController() =>
    //     (ICollectionDragAndDropController)new ListViewReorderableDragAndDropController(this);

    /// <summary>
    ///        <para>
    /// Creates a BaseListView with all default properties. The BaseHorizontalCollectionView.itemsSource
    /// must all be set for the BaseListView to function properly.
    /// </para>
    ///      </summary>
    public BaseListViewH() => this.AddToClassList(BaseListView.ussClassName);

    /// <summary>
    ///        <para>
    /// Constructs a BaseListView, with all important properties provided.
    /// </para>
    ///      </summary>
    /// <param name="itemsSource">The list of items to use as a data source.</param>
    /// <param name="itemWidth">The width of each item, in pixels.</param>
    public BaseListViewH(IList itemsSource, float itemWidth = -1f)
        : base(itemsSource, itemWidth)
    {
        this.AddToClassList(BaseListView.ussClassName);
    }

    private protected override void PostRefresh()
    {
        this.UpdateArraySizeField();
        this.UpdateListViewLabel();
        base.PostRefresh();
    }

    /// <summary>
    ///        <para>
    /// Defines UxmlTraits for the BaseListView.
    /// </para>
    ///      </summary>
    public new class UxmlTraits : BaseHorizontalCollectionView.UxmlTraits
    {
        private readonly UxmlBoolAttributeDescription m_ShowFoldoutHeader;
        private readonly UxmlStringAttributeDescription m_HeaderTitle;
        private readonly UxmlBoolAttributeDescription m_ShowAddRemoveFooter;
        private readonly UxmlEnumAttributeDescription<ListViewReorderMode> m_ReorderMode;
        private readonly UxmlBoolAttributeDescription m_ShowBoundCollectionSize;

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
        /// Initializes BaseListView properties using values from the attribute bag.
        /// </para>
        ///      </summary>
        /// <param name="ve">The object to initialize.</param>
        /// <param name="bag">The attribute bag.</param>
        /// <param name="cc">The creation context; unused.</param>
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            BaseListViewH baseListView = (BaseListViewH)ve;
            baseListView.reorderMode = this.m_ReorderMode.GetValueFromBag(bag, cc);
            baseListView.showFoldoutHeader = this.m_ShowFoldoutHeader.GetValueFromBag(bag, cc);
            baseListView.headerTitle = this.m_HeaderTitle.GetValueFromBag(bag, cc);
            baseListView.showAddRemoveFooter = this.m_ShowAddRemoveFooter.GetValueFromBag(bag, cc);
            baseListView.showBoundCollectionSize = this.m_ShowBoundCollectionSize.GetValueFromBag(bag, cc);
        }

        public UxmlTraits()
        {
            UxmlBoolAttributeDescription attributeDescription1 = new UxmlBoolAttributeDescription();
            attributeDescription1.name = "show-foldout-header";
            attributeDescription1.defaultValue = false;
            this.m_ShowFoldoutHeader = attributeDescription1;
            UxmlStringAttributeDescription attributeDescription2 = new UxmlStringAttributeDescription();
            attributeDescription2.name = "header-title";
            attributeDescription2.defaultValue = string.Empty;
            this.m_HeaderTitle = attributeDescription2;
            UxmlBoolAttributeDescription attributeDescription3 = new UxmlBoolAttributeDescription();
            attributeDescription3.name = "show-add-remove-footer";
            attributeDescription3.defaultValue = false;
            this.m_ShowAddRemoveFooter = attributeDescription3;
            UxmlEnumAttributeDescription<ListViewReorderMode> attributeDescription4 =
                new UxmlEnumAttributeDescription<ListViewReorderMode>();
            attributeDescription4.name = "reorder-mode";
            attributeDescription4.defaultValue = ListViewReorderMode.Simple;
            this.m_ReorderMode = attributeDescription4;
            UxmlBoolAttributeDescription attributeDescription5 = new UxmlBoolAttributeDescription();
            attributeDescription5.name = "show-bound-collection-size";
            attributeDescription5.defaultValue = true;
            this.m_ShowBoundCollectionSize = attributeDescription5;
        }
    }
}
