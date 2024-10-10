using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteControl : MonoBehaviour, INeedInjection
{
    public VisualTreeAsset songEntryUi;

    [InjectedInInspector]
    public bool dynamicListViewItemsSize;

    [FormerlySerializedAs("targetTransitionToSelectedItemTimeInSeconds")] [InjectedInInspector]
    public float maxTransitionToSelectedItemTimeInSeconds = 0.5f;

    [Inject]
    private Injector injector;

    [Inject]
    private SongSelectSongPreviewControl songPreviewControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Settings settings;

    // TODO: searchControl should not be known to this class
    [Inject]
    private SongSearchControl songSearchControl;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListViewH songListView;

    private List<SongSelectEntry> entries = new();
    public IReadOnlyList<SongSelectEntry> Entries => entries;

    // private List<SongMeta> songs = new();
    // public IReadOnlyList<SongMeta> Songs => songs;

    public IReactiveProperty<SongSelectEntrySelection> Selection { get; private set; } = new ReactiveProperty<SongSelectEntrySelection>();

    private readonly Subject<SongSelectEntrySelection> selectionClickedEventStream = new();
    public IObservable<SongSelectEntrySelection> SelectionClickedEventStream => selectionClickedEventStream;

    private readonly Subject<List<SongSelectEntry>> entryListChangedEventStream = new();
    public IObservable<List<SongSelectEntry>> EntryListChangedEventStream => entryListChangedEventStream;

    private readonly Subject<SongSelectEntry> submitEventStream = new();
    public IObservable<SongSelectEntry> SubmitEventStream => submitEventStream;

    private ScrollView songListViewScrollView;
    private int DummyScrollViewItemCountPerSide => dynamicListViewItemsSize
        ? 3
        : 0;

    public int SelectedEntryIndex
    {
        get
        {
            return Selection.Value.Entry != null
                ? Selection.Value.Index
                : -1;
        }
    }

    public SongSelectEntry SelectedEntry
    {
        get
        {
            return Selection.Value.Entry;
        }
    }

    private readonly List<SongSelectEntryControl> entryControls = new();
    public IReadOnlyList<SongSelectEntryControl> EntryControls => entryControls;
    public SongSelectEntryControl SelectedEntryControl => entryControls
        .FirstOrDefault(it => it.SongSelectEntry == SelectedEntry);

    private readonly Subject<SongSelectEntryControl> createdSongSelectEntryControlEventStream = new();
    public IObservable<SongSelectEntryControl> CreatedSongSelectEntryControlEventStream => createdSongSelectEntryControlEventStream;
    public IReadOnlyList<SongSelectSongEntry> SongEntries => entries.OfType<SongSelectSongEntry>().ToList();

    private bool isInitialized;

    private bool isPointerDownOnListView;

    private float lastPlaySongSelectSoundEffectTimeInSeconds;

    private float transitionToSelectedItemTimeInSeconds;
    private float transitionStartScrollOffsetX;

    private SongSelectEntry initiallySelectedEntry;

    private Vector2 lastScrollOffset;

    private void Start()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongRouletteControl.Start");

        songListViewScrollView = songListView.Q<ScrollView>();

        songListView.RegisterCallback<WheelEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
        songListView.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (entries.IsNullOrEmpty())
            {
                return;
            }

            if ((evt.keyCode == KeyCode.End && Selection.Value.Index == entries.Count - 1)
                || (evt.keyCode == KeyCode.Home && Selection.Value.Index == 0))
            {
                // Already selected the first / last item
                evt.StopImmediatePropagation();
            }
        }, TrickleDown.TrickleDown);
        songListView.RegisterCallback<PointerDownEvent>(_ =>
        {
            isPointerDownOnListView = true;
        }, TrickleDown.TrickleDown);

        songListView.RegisterCallback<NavigationSubmitEvent>(_ =>
        {
            if (songSearchControl.IsSearching)
            {
                songSearchControl.SubmitSearch();
                return;
            }

            if (SelectedEntry != null)
            {
                submitEventStream.OnNext(SelectedEntry);
            }
        }, TrickleDown.TrickleDown);

        // Hide scroll bars
        songListViewScrollView.horizontalScrollerVisibility = settings.ShowScrollBarInSongSelect
            ? ScrollerVisibility.Auto
            : ScrollerVisibility.Hidden;
        songListViewScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        songListView.makeItem = OnMakeItem;
        songListView.bindItem = OnBindItem;
        songListView.unbindItem = OnUnbindItem;
        songListView.selectedIndicesChanged += OnSongListViewSelectionIndexChanged;

        InitSelectionSoundEffect();

        isInitialized = true;

        // Populate the list with the songs that were set before the control was initialized.
        if (!entries.IsNullOrEmpty())
        {
            SetEntries(entries);
        }

        SelectInitialEntry();
    }

    private void SelectInitialEntry()
    {
        if (initiallySelectedEntry == null
            || entries.IsNullOrEmpty()
            || !entries.Contains(initiallySelectedEntry))
        {
            return;
        }

        if (VisualElementUtils.HasGeometry(songListView))
        {
            DoSelectInitialEntry();
        }
        else
        {
            songListView.RegisterHasGeometryCallbackOneShot(_ => DoSelectInitialEntry());
        }
    }

    private void DoSelectInitialEntry()
    {
        SelectEntry(initiallySelectedEntry);
        transitionStartScrollOffsetX = songListViewScrollView.scrollOffset.x;
        transitionToSelectedItemTimeInSeconds = -maxTransitionToSelectedItemTimeInSeconds;
    }

    private void OnUnbindItem(VisualElement element, int index)
    {
        SongSelectEntry entry = element.userData as SongSelectEntry;
        if (entry == null)
        {
            return;
        }
        element.userData = null;

        SongSelectEntryControl songSelectEntryControl = entryControls.FirstOrDefault(it => it.SongSelectEntry == entry);
        if (songSelectEntryControl != null)
        {
            songSelectEntryControl.Dispose();
            entryControls.Remove(songSelectEntryControl);
        }
    }

    private void OnBindItem(VisualElement element, int index)
    {
        if (index < DummyScrollViewItemCountPerSide
            || index >= (entries.Count + DummyScrollViewItemCountPerSide))
        {
            element.HideByVisibility();
            // element.style.opacity = 0.33f;
            return;
        }
        element.ShowByVisibility();
        // element.style.opacity = 1;

        SongSelectEntry entry = entries[index - DummyScrollViewItemCountPerSide];
        element.userData = entry;
        CreateEntryControl(entry, element);
        if (songListView.selectedIndex == index)
        {
            ApplyThemeStyleUtils.SetListViewItemActive(songListView, element, true);
        }
        else
        {
            ApplyThemeStyleUtils.SetListViewItemActive(songListView, element, false);
        }
    }

    private VisualElement OnMakeItem()
    {
        VisualElement songEntryVisualElement = songEntryUi.CloneTree().Children().FirstOrDefault();
        return songEntryVisualElement;
    }

    private void InitSelectionSoundEffect()
    {
        Selection.Subscribe(_ => PlaySelectionSoundEffect());
    }

    private void PlaySelectionSoundEffect()
    {
        if (Time.time < lastPlaySongSelectSoundEffectTimeInSeconds + 0.1f)
        {
            return;
        }

        lastPlaySongSelectSoundEffectTimeInSeconds = Time.time;
        SfxManager.PlaySongSelectSound();
    }

    private void Update()
    {
        if (!dynamicListViewItemsSize)
        {
            return;
        }

        if (!InputUtils.IsPointerDown())
        {
            if (isPointerDownOnListView)
            {
                // Reset the transition time on pointer up.
                transitionToSelectedItemTimeInSeconds = maxTransitionToSelectedItemTimeInSeconds;
                transitionStartScrollOffsetX = songListViewScrollView.scrollOffset.x;
            }
            isPointerDownOnListView = false;
            UpdateScrollSelectedListViewItemToCenter();
        }

        entryControls.ForEach(entryControl => entryControl.Update());
    }

    private void LateUpdate()
    {
        // React to changed scrollOffset in LateUpdated to ensure Unity has calculated the UI layout.
        if (Math.Abs(lastScrollOffset.x - songListViewScrollView.scrollOffset.x) > 0.1f)
        {
            lastScrollOffset = songListViewScrollView.scrollOffset;
            OnScrollOffsetChanged();
        }
    }

    private void UpdateScrollSelectedListViewItemToCenter()
    {
        if (songListView == null
            || songListViewScrollView == null
            || songListView.itemsSource == null
            || !VisualElementUtils.HasGeometry(songListView)
            || !VisualElementUtils.HasGeometry(songListViewScrollView))
        {
            return;
        }

        float selectedItemCenterX = ((songListView.selectedIndex + 1) * songListView.fixedItemWidth) - (songListView.fixedItemWidth / 2);
        Vector2 listViewCenter = songListView.localBound.center;
        float targetScrollOffsetX = selectedItemCenterX - listViewCenter.x;
        targetScrollOffsetX = NumberUtils.Limit(targetScrollOffsetX, 0, songListView.fixedItemWidth * songListView.itemsSource.Count);
        float scrollOffsetDistance = Mathf.Abs(targetScrollOffsetX - transitionStartScrollOffsetX);
        if (transitionToSelectedItemTimeInSeconds > 0)
        {
            transitionToSelectedItemTimeInSeconds -= Time.deltaTime;
        }
        float transitionFactor = 1 - (transitionToSelectedItemTimeInSeconds / maxTransitionToSelectedItemTimeInSeconds);
        if (transitionFactor is > 0 and < 1
            && scrollOffsetDistance < songListView.contentRect.width * 2)
        {
            float interpolatedScrollOffsetX = LeanTween.easeOutCubic(transitionStartScrollOffsetX, targetScrollOffsetX, transitionFactor);
            songListViewScrollView.scrollOffset = new Vector2(
                interpolatedScrollOffsetX,
                songListViewScrollView.scrollOffset.y);
        }
        else if (Mathf.Abs(songListViewScrollView.scrollOffset.x - targetScrollOffsetX) > 0.01f)
        {
            songListViewScrollView.scrollOffset = new Vector2(
                targetScrollOffsetX,
                songListViewScrollView.scrollOffset.y);
        }
    }

    private void SelectListViewItemClosestToCenter()
    {
        List<VisualElement> listViewItems = songListView.Query(null, "unity-collection-view__item").ToList();
        VisualElement listViewItemClosestToTheCenter = listViewItems.FindMinElement(listViewItem => Mathf.Abs(listViewItem.worldBound.center.x - songListView.worldBound.center.x));
        if (listViewItemClosestToTheCenter != null)
        {
            SongSelectEntry entry = listViewItemClosestToTheCenter.userData as SongSelectSongEntry;
            if (entry != null
                && SelectedEntry != entry)
            {
                 SelectEntry(entry);
            }
        }
    }

    private void OnScrollOffsetChanged()
    {
        if (!dynamicListViewItemsSize)
        {
            return;
        }

        UpdateListViewItemPositions();

        // Move selected list view item to the center
        if (isPointerDownOnListView)
        {
            // Select list view item that is closest to the center
            SelectListViewItemClosestToCenter();
        }
    }

    private void UpdateListViewItemPositions()
    {
        if (!dynamicListViewItemsSize
            || !VisualElementUtils.HasGeometry(songListView))
        {
            return;
        }

        List<VisualElement> listViewItems = songListView.Query(null, "unity-collection-view__item").ToList();
        float maxDistanceToCenter = songListView.worldBound.width / 2f;
        float songListCenterX = songListView.worldBound.center.x;
        float maxOffset = -50;
        float maxScaleOffset = 0.5f;
        foreach (VisualElement listViewItem in listViewItems)
        {
            if (!VisualElementUtils.HasGeometry(listViewItem))
            {
                continue;
            }

            float horizontalDistanceToCenter = Mathf.Abs(listViewItem.worldBound.center.x - songListCenterX);
            float distanceFactor = horizontalDistanceToCenter / maxDistanceToCenter;
            distanceFactor = NumberUtils.Limit(distanceFactor, 0, 1);
            listViewItem.style.top = maxOffset * distanceFactor;
            float scale = 1 - maxScaleOffset * distanceFactor;
            listViewItem.style.scale = new StyleScale(new Vector2(scale, scale));
        }
    }

    private void OnSongListViewSelectionIndexChanged(IEnumerable<int> selectedIndexes)
    {
        int selectedIndex = selectedIndexes.FirstOrDefault();
        if (selectedIndex < DummyScrollViewItemCountPerSide
            && entries.Count > 0)
        {
            SetSelectionAndScrollToIndex(0);
            return;
        }

        if(selectedIndex >= entries.Count + DummyScrollViewItemCountPerSide
           && entries.Count > 0)
        {
            SetSelectionAndScrollToIndex(entries.Count - 1);
            return;
        }

        SongSelectEntry selectedEntry = entries.ElementAtOrDefault(selectedIndex - DummyScrollViewItemCountPerSide);
        SelectEntry(selectedEntry);
    }

    private void CreateEntryControl(SongSelectEntry entry, VisualElement songEntryVisualElement)
    {
        SongSelectEntryControl item = injector
            .WithRootVisualElement(songEntryVisualElement)
            .CreateAndInject<SongSelectEntryControl>();
        item.SongSelectEntry = entry;

        if (entry is SongSelectSongEntry songEntry)
        {
            item.Name = songEntry.SongMeta.GetArtistDashTitle();
        }
        else if (entry is SongSelectFolderEntry folderEntry)
        {
            item.Name = folderEntry.DirectoryInfo.Name;
        }

        item.ClickOnSongImageEventStream.Subscribe(_ => OnEntryClicked(entry));

        entryControls.Add(item);

        createdSongSelectEntryControlEventStream.OnNext(item);
    }

    public void SetEntries(IReadOnlyCollection<SongSelectEntry> newEntries)
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectRouletteControl.SetEntries");

        SongSelectEntry lastSelectedEntry = SelectedEntry;
        entries = new List<SongSelectEntry>(newEntries);

        if (!isInitialized)
        {
            // Remember these entries but do not populate the list yet.
            return;
        }

        RestoreLastSelection(lastSelectedEntry);

        if (!VisualElementUtils.HasGeometry(songListView))
        {
            songListView.RegisterCallbackOneShot<GeometryChangedEvent>(_ => UpdateListViewItems());
        }
        else
        {
            UpdateListViewItems();
        }

        entryListChangedEventStream.OnNext(entries);
    }

    private void RestoreLastSelection(SongSelectEntry lastSelectedEntry)
    {
        if (entries.IsNullOrEmpty())
        {
            Selection.Value = new SongSelectEntrySelection(null, -1, 0);
            return;
        }

        int restoredSelectedIndex = entries.IndexOf(lastSelectedEntry);
        if (restoredSelectedIndex >= 0)
        {
            Selection.Value = new SongSelectEntrySelection(entries[restoredSelectedIndex], restoredSelectedIndex, entries.Count);
            return;
        }

        Selection.Value = new SongSelectEntrySelection(entries.FirstOrDefault(), 0, entries.Count);
    }

    private void UpdateListViewItems()
    {
        List<object> itemsSource = new List<object>(entries);
        if (dynamicListViewItemsSize)
        {
            // Add dummy items such that the selected actual list view item can be scrolled to the center
            for (int i = 0; i < DummyScrollViewItemCountPerSide; i++)
            {
                itemsSource.Insert(0, new object());
                itemsSource.Add(new object());
            }
        }
        songListView.itemsSource = itemsSource;
        songListView.RefreshItems();
        if (SelectedEntry != null)
        {
            SetSelectionAndScrollToIndex(SelectedEntryIndex);
        }
    }

    private void SetSelectionAndScrollToIndex(int songIndex)
    {
        int listViewItemIndex = songIndex + DummyScrollViewItemCountPerSide;
        if (songListView.selectedIndex == listViewItemIndex)
        {
            return;
        }
        songListView.SetSelection(listViewItemIndex);

        if (!dynamicListViewItemsSize)
        {
            songListView.ScrollToItem(listViewItemIndex);
        }
    }

    private int GetEntryIndex(SongSelectEntry entry)
    {
        return entries.IndexOf(entry);
    }

    public SongSelectEntry GetEntryBySongMeta(SongMeta songMeta)
    {
        SongSelectEntry matchingEntry = entries.FirstOrDefault(entry => entry is SongSelectSongEntry songEntry
                                                                        && songEntry.SongMeta == songMeta);
        return matchingEntry;
    }

    public int GetEntryIndexBySongMeta(SongMeta songMeta)
    {
        SongSelectEntry matchingEntry = GetEntryBySongMeta(songMeta);
        if (matchingEntry == null)
        {
            return -1;
        }
        return entries.IndexOf(matchingEntry);
    }

    public void SelectEntryBySongMeta(SongMeta songMeta)
    {
        SelectEntry(GetEntryBySongMeta(songMeta));
    }

    public void SelectEntry(SongSelectEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if(songListViewScrollView == null)
        {
            initiallySelectedEntry = entry;
            return;
        }

        int index = entries.IndexOf(entry);
        if (SelectedEntry == entry
            && SelectedEntryIndex == index)
        {
            // Nothing to change
            return;
        }

        transitionToSelectedItemTimeInSeconds = maxTransitionToSelectedItemTimeInSeconds;
        transitionStartScrollOffsetX = songListViewScrollView.scrollOffset.x;
        SetSelectionAndScrollToIndex(index);
        Selection.Value = new SongSelectEntrySelection(entry, index, entries.Count);
    }

    public void SelectEntryByIndex(int index, bool wrapAround = true)
    {
        if (!wrapAround
            && (index < 0 || entries.Count <= index))
        {
            // Ignore out-of-range index
            return;
        }

        SongSelectEntry nextEntry = GetEntryAtIndex(index);
        SelectEntry(nextEntry);
    }

    public SongSelectEntry Find(Predicate<SongSelectEntry> predicate)
    {
        return entries.Find(predicate);
    }

    public void SelectNextEntry()
    {
        int nextIndex;
        if (SelectedEntryIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = SelectedEntryIndex + 1;
        }
        SelectEntryByIndex(nextIndex);
    }

    public void SelectPreviousEntry()
    {
        int nextIndex;
        if (SelectedEntryIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = SelectedEntryIndex - 1;
        }
        SelectEntryByIndex(nextIndex);
    }

    public void SelectVeryLastEntry()
    {
        SelectEntryByIndex(entries.Count - 1);
    }

    public void SelectVeryFirstEntry()
    {
        SelectEntryByIndex(0);
    }

    public SongSelectEntry GetEntryAtIndex(int index)
    {
        if (entries.Count == 0)
        {
            return null;
        }
        int wrappedIndex = (index < 0) ? index + entries.Count : index;
        int wrappedIndexModulo = wrappedIndex % entries.Count;
        if (wrappedIndexModulo < 0)
        {
            wrappedIndexModulo = 0;
        }
        SongSelectEntry entry = entries[wrappedIndexModulo];
        return entry;
    }

    private void OnEntryClicked(SongSelectEntry entry)
    {
        if (SelectedEntry != null
            && SelectedEntry == entry)
        {
            selectionClickedEventStream.OnNext(Selection.Value);
        }
        else
        {
            SelectEntry(entry);
        }
    }

    public void Focus()
    {
        songListView.Focus();
    }

    public void OpenSelectedEntryContextMenu()
    {
        if (SelectedEntryControl == null)
        {
            return;
        }

        SelectedEntryControl.OpenContextMenu();
    }
}
