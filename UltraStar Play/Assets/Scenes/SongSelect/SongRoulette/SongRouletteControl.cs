using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteControl : MonoBehaviour, INeedInjection
{
    public VisualTreeAsset songEntryUi;

    [InjectedInInspector]
    public bool dynamicListViewItemsSize;
    
    [Inject]
    private Injector injector;

    [Inject]
    private SongSelectSongPreviewControl songPreviewControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListViewH songListView;
    
    private List<SongMeta> songs = new();
    public IReadOnlyList<SongMeta> Songs => songs;

    public IReactiveProperty<SongSelection> Selection { get; private set; } = new ReactiveProperty<SongSelection>();

    private readonly Subject<SongSelection> selectionClickedEventStream = new();
    public IObservable<SongSelection> SelectionClickedEventStream => selectionClickedEventStream;

    private readonly Subject<List<SongMeta>> songListChangedEventStream = new();
    public IObservable<List<SongMeta>> SongListChangedEventStream => songListChangedEventStream;

    private ScrollView songListViewScrollView;
    private int dummyScrollViewItemCountPerSide = 2;
    
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

    private void Start()
    {
        songListView.RegisterCallback<WheelEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
        
        // Hide scroll bars
        songListViewScrollView = songListView.Q<ScrollView>();
        songListViewScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        songListViewScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

        songListView.makeItem = () =>
        {
            VisualElement songEntryVisualElement = songEntryUi.CloneTree().Children().FirstOrDefault();
            ThemeManager.ApplyThemeSpecificStylesToVisualElements(songEntryVisualElement);
            return songEntryVisualElement;
        };
        songListView.bindItem = (VisualElement element, int index) =>
        {
            if (index < dummyScrollViewItemCountPerSide
                || index >= (songs.Count + dummyScrollViewItemCountPerSide))
            {
                element.HideByVisibility();
                // element.style.opacity = 0.33f;
                return;
            }
            element.ShowByVisibility();
            // element.style.opacity = 1;
            
            SongMeta songMeta = songs[index - dummyScrollViewItemCountPerSide];
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
        };
        songListView.unbindItem = (VisualElement element, int index) =>
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
        };
        songListView.selectedIndicesChanged += OnSongListViewSelectionIndexChanged;

        songListView.Q<ScrollView>().ObserveEveryValueChanged(scrollView => scrollView.scrollOffset)
            .Subscribe(_ => OnScrollOffsetChanged());
        
        isInitialized = true;
        
        // Populate the list with the songs that were set before the control was initialized.
        if (!songs.IsNullOrEmpty())
        {
            SetSongs(songs);
        }
    }

    private void Update()
    {
        if (!dynamicListViewItemsSize)
        {
            return;
        }

        VisualElement selectedListViewItem = songListView.Q(null, "unity-collection-view__item--selected");
        // Debug.Log("selectedListViewItem: " + selectedListViewItem);
        if (selectedListViewItem != null
            && !InputUtils.IsPointerDown())
        {
            Vector2 currentCenter = selectedListViewItem.worldBound.center;
            Vector2 targetCenter = songListView.worldBound.center;
            Vector2 delta = targetCenter - currentCenter;
            Vector2 step = delta * 0.1f;
            if (Mathf.Abs(delta.x) > 0.5f
                && Mathf.Abs(step.x) < songListView.worldBound.width)
            {
                songListViewScrollView.scrollOffset = new Vector2(
                    songListViewScrollView.scrollOffset.x - step.x,
                    songListViewScrollView.scrollOffset.y);
            }
            else if (Mathf.Abs(delta.x) > 0.5f)
            {
                songListViewScrollView.scrollOffset = new Vector2(
                    songListViewScrollView.scrollOffset.x - delta.x,
                    songListViewScrollView.scrollOffset.y);
            }
        }

        songListView.RegisterCallback<PointerDownEvent>(_ =>
        {
            isPointerDownOnListView = true;
        });
        if (!InputUtils.IsPointerDown())
        {
            isPointerDownOnListView = false;
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
        if (!dynamicListViewItemsSize)
        {
            return;
        }
        
        List<VisualElement> listViewItems = songListView.Query(null, "unity-collection-view__item").ToList();
        float maxDistanceToCenter = songListView.worldBound.width / 2f;
        float maxOffset = -50;
        float maxScaleOffset = 0.5f;
        foreach (VisualElement listViewItem in listViewItems)
        {
            float horizontalDistanceToCenter = Mathf.Abs(listViewItem.worldBound.center.x - songListView.worldBound.center.x);
            float distanceFactor = horizontalDistanceToCenter / maxDistanceToCenter;
            listViewItem.style.top = maxOffset * distanceFactor;
            float scale = 1 - maxScaleOffset * distanceFactor;
            listViewItem.style.scale = new StyleScale(new Vector2(scale, scale));
        }
    }

    private void OnSongListViewSelectionIndexChanged(IEnumerable<int> selectedIndexes)
    {
        int selectedIndex = selectedIndexes.FirstOrDefault();
        if (selectedIndex < dummyScrollViewItemCountPerSide
            && songs.Count > 0)
        {
            SetSelectionAndScrollToSongIndex(0);
            return;
        }
        
        if(selectedIndex >= songs.Count + dummyScrollViewItemCountPerSide
           && songs.Count > 0)
        {
            SetSelectionAndScrollToSongIndex(songs.Count - 1);
            return;
        }
        
        SongMeta selectedSongMeta = songs.ElementAtOrDefault(selectedIndex - dummyScrollViewItemCountPerSide);
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
            for (int i = 0; i < dummyScrollViewItemCountPerSide; i++)
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

        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => UpdateListViewItemPositions()));
    }

    private void SetSelectionAndScrollToSongIndex(int songIndex)
    {
        songListView.SetSelection(songIndex + dummyScrollViewItemCountPerSide);
        
        if (!dynamicListViewItemsSize)
        {
            songListView.ScrollToItem(songIndex + dummyScrollViewItemCountPerSide);
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

        int songIndex = songs.IndexOf(songMeta);
        if (songListView.selectedIndex != songIndex)
        {
            SetSelectionAndScrollToSongIndex(songIndex);
        }
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
