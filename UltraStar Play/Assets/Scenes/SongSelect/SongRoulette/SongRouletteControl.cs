using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

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

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListViewH songListView;
    
    private List<SongMeta> songs = new();
    public IReadOnlyList<SongMeta> Songs => songs;

    public IReactiveProperty<SongSelection> Selection { get; private set; } = new ReactiveProperty<SongSelection>();

    private readonly Subject<SongSelection> selectionClickedEventStream = new();
    public IObservable<SongSelection> SelectionClickedEventStream => selectionClickedEventStream;

    private readonly Subject<List<SongMeta>> songListChangedEventStream = new();
    public IObservable<List<SongMeta>> SongListChangedEventStream => songListChangedEventStream;
    
    private readonly Subject<SongMeta> submitEventStream = new();
    public IObservable<SongMeta> SubmitEventStream => submitEventStream;

    private ScrollView songListViewScrollView;
    private int DummyScrollViewItemCountPerSide => dynamicListViewItemsSize
        ? 3
        : 0;
    
    private int SelectedSongIndex
    {
        get
        {
            return (Selection.Value.SongMeta == null) ? -1 : Selection.Value.SongIndex;
        }
    }

    private SongMeta SelectedSongMeta
    {
        get
        {
            return Selection.Value.SongMeta;
        }
    }
    
    private readonly List<SongEntryControl> songEntryControls = new();
    public IReadOnlyList<SongEntryControl> SongEntryControls => songEntryControls;
    public SongEntryControl SelectedSongEntryControl => songEntryControls
        .FirstOrDefault(it => it.SongMeta == Selection.Value.SongMeta);
    
    private bool isInitialized;

    private bool isPointerDownOnListView;

    private float lastPlaySongSelectSoundEffectTimeInSeconds;
    
    private float transitionToSelectedItemTimeInSeconds;
    private float transitionStartScrollOffsetX;

    private SongMeta initiallySelectedSongMeta;

    private Vector2 lastScrollOffset;
    
    private void Start()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongRouletteControl.Start");
        
        songListViewScrollView = songListView.Q<ScrollView>();
        
        songListView.RegisterCallback<WheelEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
        songListView.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (songs.IsNullOrEmpty())
            {
                return;
            }
            
            if ((evt.keyCode == KeyCode.End && Selection.Value.SongIndex == songs.Count - 1) 
                || (evt.keyCode == KeyCode.Home && Selection.Value.SongIndex == 0))
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
            if (SelectedSongMeta != null)
            {
                submitEventStream.OnNext(SelectedSongMeta);
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

        InitSongSelectSoundEffect();
        
        isInitialized = true;
        
        // Populate the list with the songs that were set before the control was initialized.
        if (!songs.IsNullOrEmpty())
        {
            SetSongs(songs);
        }

        SelectInitialSongMeta();
    }

    private void SelectInitialSongMeta()
    {
        if (initiallySelectedSongMeta == null
            || songs.IsNullOrEmpty()
            || !songs.Contains(initiallySelectedSongMeta))
        {
            return;
        }
        
        if (VisualElementUtils.HasGeometry(songListView))
        {
            DoSelectInitialSongMeta();
        }
        else
        {
            songListView.RegisterHasGeometryCallbackOneShot(_ => DoSelectInitialSongMeta());
        }
    }
    
    private void DoSelectInitialSongMeta()
    {
        SelectSong(initiallySelectedSongMeta);
        transitionStartScrollOffsetX = songListViewScrollView.scrollOffset.x;
        transitionToSelectedItemTimeInSeconds = -maxTransitionToSelectedItemTimeInSeconds;
    }

    private void OnUnbindItem(VisualElement element, int index)
    {
        SongMeta songMeta = element.userData as SongMeta;
        if (songMeta == null)
        {
            return;
        }
        element.userData = null;
        
        SongEntryControl songEntryControl = songEntryControls.FirstOrDefault(it => it.SongMeta == songMeta);
        if (songEntryControl != null)
        {
            songEntryControl.Dispose();
            songEntryControls.Remove(songEntryControl);
        }
    }

    private void OnBindItem(VisualElement element, int index)
    {
        if (index < DummyScrollViewItemCountPerSide
            || index >= (songs.Count + DummyScrollViewItemCountPerSide))
        {
            element.HideByVisibility();
            // element.style.opacity = 0.33f;
            return;
        }
        element.ShowByVisibility();
        // element.style.opacity = 1;
        
        SongMeta songMeta = songs[index - DummyScrollViewItemCountPerSide];
        element.userData = songMeta;
        CreateSongEntryControl(songMeta, element);
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
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(songEntryVisualElement);
        return songEntryVisualElement;
    }

    private void InitSongSelectSoundEffect()
    {
        Selection.Subscribe(_ => PlaySelectSongSoundEffect());
    }

    private void PlaySelectSongSoundEffect()
    {
        if (Time.time < lastPlaySongSelectSoundEffectTimeInSeconds + 0.1f)
        {
            return;
        }

        lastPlaySongSelectSoundEffectTimeInSeconds = Time.time;
        AudioManager.PlaySongSelectSound();
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
            SongMeta songMeta = listViewItemClosestToTheCenter.userData as SongMeta;
            if (songMeta != null
                && SelectedSongMeta != songMeta)
            {
                 SelectSong(songMeta);
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
            && songs.Count > 0)
        {
            SetSelectionAndScrollToSongIndex(0);
            return;
        }
        
        if(selectedIndex >= songs.Count + DummyScrollViewItemCountPerSide
           && songs.Count > 0)
        {
            SetSelectionAndScrollToSongIndex(songs.Count - 1);
            return;
        }
        
        SongMeta selectedSongMeta = songs.ElementAtOrDefault(selectedIndex - DummyScrollViewItemCountPerSide);
        SelectSong(selectedSongMeta);
    }

    private void CreateSongEntryControl(SongMeta songMeta, VisualElement songEntryVisualElement)
    {
        SongEntryControl item = injector
            .WithRootVisualElement(songEntryVisualElement)
            .CreateAndInject<SongEntryControl>();
        item.Name = songMeta.Artist + "-" + songMeta.Title;
        item.SongMeta = songMeta;
        item.ClickOnSongImageEventStream.Subscribe(_ => OnSongButtonClicked(songMeta));
            
        songEntryControls.Add(item);
    }

    public void SetSongs(IReadOnlyCollection<SongMeta> songMetas)
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongRouletteControl.SetSongs");
        
        int lastSelectedSongIndex = NumberUtils.Limit(SelectedSongIndex, 0, songMetas.Count - 1);
        SongMeta lastSelectedSongMeta = Selection.Value.SongMeta;
        songs = new List<SongMeta>(songMetas);

        if (!isInitialized)
        {
            // Remember these songs but do not populate the list yet.
            return;
        }
        
        if (songs.Count > 0)
        {
            // Try to restore song selection
            if (lastSelectedSongMeta != null)
            {
                int songIndex = GetSongIndex(lastSelectedSongMeta);
                if (songIndex >= 0)
                {
                    lastSelectedSongIndex = songIndex;
                }
            }

            Selection.Value = new SongSelection(songs[lastSelectedSongIndex], lastSelectedSongIndex, songs.Count);
        }
        else
        {
            Selection.Value = new SongSelection(null, -1, 0);
        }

        if (!VisualElementUtils.HasGeometry(songListView))
        {
            songListView.RegisterCallbackOneShot<GeometryChangedEvent>(_ => UpdateListViewItems());
        }
        else
        {
            UpdateListViewItems();
        }
        
        songListChangedEventStream.OnNext(songs);
    }

    private void UpdateListViewItems()
    {
        List<object> itemsSource = new List<object>(songs);
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
        if (Selection.Value.SongMeta != null)
        {
            SetSelectionAndScrollToSongIndex(Selection.Value.SongIndex);
        }
    }

    private void SetSelectionAndScrollToSongIndex(int songIndex)
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
    
    private int GetSongIndex(SongMeta songMeta)
    {
        return songs.IndexOf(songMeta);
    }

    public void SelectSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }
        
        if(songListViewScrollView == null)
        {
            initiallySelectedSongMeta = songMeta;
            return;
        }

        int songIndex = songs.IndexOf(songMeta);
        if (Selection.Value.SongMeta == songMeta
            && Selection.Value.SongIndex == songIndex)
        {
            // Nothing to change
            return;
        }

        transitionToSelectedItemTimeInSeconds = maxTransitionToSelectedItemTimeInSeconds;
        transitionStartScrollOffsetX = songListViewScrollView.scrollOffset.x;
        SetSelectionAndScrollToSongIndex(songIndex);
        Selection.Value = new SongSelection(songMeta, songIndex, songs.Count);
    }

    public void SelectSongByIndex(int index, bool wrapAround = true)
    {
        if (!wrapAround
            && (index < 0 || songs.Count <= index))
        {
            // Ignore out-of-range index
            return;
        }

        SongMeta nextSong = GetSongAtIndex(index);
        SelectSong(nextSong);
    }

    public SongMeta Find(Predicate<SongMeta> predicate)
    {
        return songs.Find(predicate);
    }

    public void SelectNextSong()
    {
        int nextIndex;
        if (SelectedSongIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = SelectedSongIndex + 1;
        }
        SelectSongByIndex(nextIndex);
    }

    public void SelectPreviousSong()
    {
        int nextIndex;
        if (SelectedSongIndex < 0)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = SelectedSongIndex - 1;
        }
        SelectSongByIndex(nextIndex);
    }

    public void SelectVeryLastSong()
    {
        SelectSongByIndex(songs.Count - 1);
    }
    
    public void SelectVeryFirstSong()
    {
        SelectSongByIndex(0);
    }
    
    public SongMeta GetSongAtIndex(int index)
    {
        if (songs.Count == 0)
        {
            return null;
        }
        int wrappedIndex = (index < 0) ? index + songs.Count : index;
        int wrappedIndexModulo = wrappedIndex % songs.Count;
        if (wrappedIndexModulo < 0)
        {
            wrappedIndexModulo = 0;
        }
        SongMeta song = songs[wrappedIndexModulo];
        return song;
    }

    private void OnSongButtonClicked(SongMeta songMeta)
    {
        if (Selection.Value.SongMeta != null
            && Selection.Value.SongMeta == songMeta)
        {
            selectionClickedEventStream.OnNext(Selection.Value);
        }
        else
        {
            SelectSong(songMeta);
        }
    }

    private T FindNearestSlot<T>(float targetPositionX, List<T> allEntries)
        where T : SongEntryPlaceholderControl
    {
        return allEntries.FindMinElement(entry =>
        {
            float x = entry.VisualElement.worldBound.center.x;
            float distance = Mathf.Abs(x - targetPositionX);
            return distance;
        });
    }

    public void Focus()
    {
        songListView.Focus();
    }
}
