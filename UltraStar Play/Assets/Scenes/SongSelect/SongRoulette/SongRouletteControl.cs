using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteControl : MonoBehaviour, INeedInjection, ITranslator
{
    private const float FlickGestureStartThresholdInPixels = 20f;
    private const float FlickGestureStopThresholdInPixels = 100f;
    private const float FlickAccelerationPerSecond = 0.0005f;
    
    public VisualTreeAsset songEntryUi;
    
    [InjectedInInspector]
    public bool showRouletteItemPlaceholders;

    public SlotListControl SlotListControl { get; private set; } = new();

    [Inject]
    private Injector injector;

    [Inject]
    private SongPreviewControl songPreviewControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.songEntryContainer)]
    private VisualElement songEntryContainer;

    [Inject(UxmlName = R.UxmlNames.songEntryPlaceholder)]
    private List<VisualElement> songEntryPlaceholders;

    private List<SongEntryPlaceholderControl> songEntryPlaceholderControls = new();

    private List<SongEntryPlaceholderControl> activeEntryPlaceholders = new();
    private SongEntryPlaceholderControl centerItem;

    private List<SongMeta> songs = new();
    public IReadOnlyList<SongMeta> Songs => songs;

    public IReactiveProperty<SongSelection> Selection { get; private set; } = new ReactiveProperty<SongSelection>();

    private readonly Subject<SongSelection> selectionClickedEventStream = new();
    public IObservable<SongSelection> SelectionClickedEventStream => selectionClickedEventStream;

    public int SongEntryPlaceholderCount => songEntryPlaceholderControls.Count;

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

    public bool IsDrag { get; private set; }
    public bool IsFlickGesture { get; private set; }
    public SongEntryControl DragSongRouletteItem { get; private set; }
    public Vector2 DragDistance { get; private set; }
    
    public float MaxAnimTimeInSeconds { get; private set; } = 0.2f;
    public float AnimTimeInSeconds { get; set; }
    public bool IsSongMenuOverlayVisible => SongEntryControls
        .AnyMatch(it => it.IsSongMenuOverlayVisible);

    private bool flickGestureWasNoTouchscreenPressed;
    private Vector2 dragVelocity;
    private float dragDuration;
    private Vector2 dragStartPosition;

    private void Start()
    {
        Selection.Subscribe(selection =>
        {
            IEnumerator CoroutineDelayedSetSelectedClass()
            {
                yield return null;
                foreach (SongEntryControl songEntryControl in songEntryControls)
                {
                    songEntryControl?.VisualElement.EnableInClassList("selected", songEntryControl.SongMeta == selection.SongMeta);
                }
            }
            StartCoroutine(CoroutineDelayedSetSelectedClass());
        });

        songEntryPlaceholderControls = songEntryPlaceholders
            .Select(it => new SongEntryPlaceholderControl(it))
            .ToList();

        // On start of first frame, the placeholders do not yet have their position and size defined correctly.
        songEntryPlaceholderControls[0].VisualElement.RegisterCallbackOneShot<GeometryChangedEvent>(evt => DoInit());
    }

    private void DoInit()
    {
        songEntryPlaceholderControls.Sort(SongEntryPlaceholderControl.comparerByCenterDistance);

        for (int i = 0; i < songEntryPlaceholderControls.Count; i++)
        {
            SongEntryPlaceholderControl current = songEntryPlaceholderControls[i];
            List<SongEntryPlaceholderControl> previousSlots = songEntryPlaceholderControls
                .Where(it => it.GetPosition().x < current.GetPosition().x)
                .ToList();
            List<SongEntryPlaceholderControl> followingSlots = songEntryPlaceholderControls
                .Where(it => it.GetPosition().x > current.GetPosition().x)
                .ToList();
            ISlotListSlot previousSlot = FindNearestSlot(current.GetPosition().x, previousSlots);
            ISlotListSlot nextSlot = FindNearestSlot(current.GetPosition().x, followingSlots);
            songEntryPlaceholderControls[i].SetNeighborSlots(previousSlot, nextSlot);
        }
        centerItem = FindNearestSlot(400, songEntryPlaceholderControls);
        activeEntryPlaceholders = new List<SongEntryPlaceholderControl>(songEntryPlaceholderControls);

        SlotListControl.SlotChangeEventStream.Subscribe(OnSongRouletteItemChangedSlot);

        if (!showRouletteItemPlaceholders)
        {
            foreach (SongEntryPlaceholderControl item in songEntryPlaceholderControls)
            {
                item.VisualElement.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden);
            }
        }

        FindActiveRouletteItems();
    }

    private void OnSongRouletteItemChangedSlot(SlotChangeEvent slotChangeEvent)
    {
        List<SongEntryPlaceholderControl> activePlaceholderControlsSortedByPosition = new(activeEntryPlaceholders);
        activePlaceholderControlsSortedByPosition.Sort(SongEntryPlaceholderControl.comparerByPosition);

        int newSelectedSongIndex = SelectedSongIndex;
        if (slotChangeEvent.Direction == ESlotListDirection.TowardsNextSlot)
        {
            newSelectedSongIndex = NumberUtils.Mod(SelectedSongIndex - 1, songs.Count);
            foreach (SongEntryControl songRouletteItem in songEntryControls)
            {
                SongEntryPlaceholderControl nextPlaceholderControl = songRouletteItem.GetCurrentSlot().GetNextSlot() as SongEntryPlaceholderControl;
                songRouletteItem.TargetPlaceholderControl = nextPlaceholderControl != null && activeEntryPlaceholders.Contains(nextPlaceholderControl)
                    ? nextPlaceholderControl
                    : null;
            }
            
            int newlyCreatedSongIndex = NumberUtils.Mod(newSelectedSongIndex - (activeEntryPlaceholders.Count / 2), songs.Count);
            CreateSongRouletteItem(songs[newlyCreatedSongIndex], activePlaceholderControlsSortedByPosition.First());
        }
        else if (slotChangeEvent.Direction == ESlotListDirection.TowardsPreviousSlot)
        {
            newSelectedSongIndex = NumberUtils.Mod(SelectedSongIndex + 1, songs.Count);
            foreach (SongEntryControl songRouletteItem in songEntryControls)
            {
                SongEntryPlaceholderControl previousPlaceholderControl = songRouletteItem.GetCurrentSlot().GetPreviousSlot() as SongEntryPlaceholderControl;
                songRouletteItem.TargetPlaceholderControl = previousPlaceholderControl != null && activeEntryPlaceholders.Contains(previousPlaceholderControl)
                    ? previousPlaceholderControl
                    : null;
            }

            int newlyCreatedSongIndex = NumberUtils.Mod(newSelectedSongIndex + (activeEntryPlaceholders.Count / 2), songs.Count);
            CreateSongRouletteItem(songs[newlyCreatedSongIndex], activePlaceholderControlsSortedByPosition.Last());
        }
        Selection.Value = new SongSelection(songs[newSelectedSongIndex] , newSelectedSongIndex, songs.Count);
    }

    void Update()
    {
        // Iterate over copy of list, because list might be modified during iteration.
        new List<SongEntryControl>(songEntryControls)
            .ForEach(it => it.Update());

        if (songs.Count == 0)
        {
            return;
        }

        if (!IsDrag
            && !IsFlickGesture
            && centerItem != null)
        {
            SpawnAndRemoveSongRouletteItems();
        }
        
        AnimTimeInSeconds += Time.deltaTime;
        AnimTimeInSeconds = NumberUtils.Limit(AnimTimeInSeconds, 0, MaxAnimTimeInSeconds);

        UpdateFlickGesture();
        
        // Initially, let all items start with full size
        if (!isInitialized)
        {
            isInitialized = true;
            foreach (SongEntryControl songRouletteItem in songEntryControls)
            {
                songRouletteItem.VisualElement.style.scale = new StyleScale(new Scale(Vector3.one));
            }
        }
    }

    private void SpawnAndRemoveSongRouletteItems()
    {
        List<SongEntryControl> usedSongRouletteItems = new();

        // Spawn roulette items for songs to be displayed (i.e. selected song and its surrounding songs)
        foreach (SongEntryPlaceholderControl placeholderControl in activeEntryPlaceholders)
        {
            int songIndex = SelectedSongIndex + placeholderControl.GetCenterDistanceIndex(activeEntryPlaceholders, centerItem);
            SongMeta songMeta = GetSongAtIndex(songIndex);

            // Get or create SongEntryControl for the song at the index
            SongEntryControl songRouletteItem = songEntryControls.FirstOrDefault(it => it.SongMeta == songMeta);
            if (songRouletteItem == null)
            {
                songRouletteItem = CreateSongRouletteItem(songMeta, placeholderControl);
            }

            // Update target
            songRouletteItem.TargetPlaceholderControl = placeholderControl;
            usedSongRouletteItems.Add(songRouletteItem);
        }

        // Remove unused items
        foreach (SongEntryControl songRouletteItem in new List<SongEntryControl>(songEntryControls))
        {
            if (!usedSongRouletteItems.Contains(songRouletteItem))
            {
                RemoveSongRouletteItem(songRouletteItem);
            }
        }
    }

    public void RemoveSongRouletteItem(SongEntryControl songRouletteItem)
    {
        songRouletteItem.Dispose();
        songEntryControls.Remove(songRouletteItem);
        SlotListControl.ListItems.Remove(songRouletteItem);
    }

    private SongEntryControl CreateSongRouletteItem(SongMeta songMeta, SongEntryPlaceholderControl placeholderControl)
    {
        // Find initial position outside of screen
        SongEntryPlaceholderControl initialPositionPlaceholderControl = placeholderControl;
        List<SongEntryPlaceholderControl> placeholderControlsSortedByPosition = new(songEntryPlaceholderControls);
        placeholderControlsSortedByPosition.Sort(SongEntryPlaceholderControl.comparerByPosition);
        if (placeholderControl.GetPosition().x > centerItem.GetPosition().x)
        {
            initialPositionPlaceholderControl = placeholderControlsSortedByPosition.Last();
        }
        else if (placeholderControl.GetPosition().x < centerItem.GetPosition().x)
        {
            initialPositionPlaceholderControl = placeholderControlsSortedByPosition.First();
        }
        Vector2 initialPosition = initialPositionPlaceholderControl.GetPosition();
        Vector2 initialSize = initialPositionPlaceholderControl.GetSize();

        SongEntryControl item = new(
            songEntryUi.CloneTree().Children().FirstOrDefault(),
            placeholderControl,
            initialPosition,
            initialSize);
        injector.WithRootVisualElement(item.VisualElement).Inject(item);
        item.Name = songMeta.Artist + "-" + songMeta.Title;
        item.SongMeta = songMeta;

        item.ClickEventStream.Subscribe(_ => OnSongButtonClicked(songMeta));

        songEntryControls.Add(item);
        SlotListControl.ListItems.Add(item);

        songEntryContainer.Add(item.VisualElement);

        return item;
    }

    private void FindActiveRouletteItems()
    {
        if (songs.Count >= songEntryPlaceholderControls.Count)
        {
            activeEntryPlaceholders = new List<SongEntryPlaceholderControl>(songEntryPlaceholderControls);
            return;
        }

        // Select the N placeholders that are closest to the center.
        activeEntryPlaceholders.Clear();
        for (int i = 0; i < songs.Count; i++)
        {
            List<SongEntryPlaceholderControl> availablePlaceholders = songEntryPlaceholderControls
                .Where(it => !activeEntryPlaceholders.Contains(it))
                .ToList();
            SongEntryPlaceholderControl nearestPlaceholder = FindNearestSlot(400, availablePlaceholders);
            activeEntryPlaceholders.Add(nearestPlaceholder);
        }
        activeEntryPlaceholders.Sort(SongEntryPlaceholderControl.comparerByCenterDistance);
    }

    public void SetSongs(IReadOnlyCollection<SongMeta> songMetas)
    {
        int lastSelectedSongIndex = NumberUtils.Limit(SelectedSongIndex, 0, songMetas.Count - 1);
        SongMeta lastSelectedSongMeta = Selection.Value.SongMeta;
        songs = new List<SongMeta>(songMetas);
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
            RemoveAllSongRouletteItems();
        }
        FindActiveRouletteItems();
    }

    private int GetSongIndex(SongMeta songMeta)
    {
        return songs.IndexOf(songMeta);
    }

    private void RemoveAllSongRouletteItems()
    {
        foreach (SongEntryControl songRouletteItem in new List<SongEntryControl>(songEntryControls))
        {
            RemoveSongRouletteItem(songRouletteItem);
        }
        songEntryControls.Clear();
    }

    public void SelectSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        Selection.Value = new SongSelection(songMeta, songs.IndexOf(songMeta), songs.Count);
        ResetAnimationTimeTowardsTargetRouletteItem();
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

    private void ResetAnimationTimeTowardsTargetRouletteItem()
    {
        songEntryControls.ForEach(it => it.StartAnimationTowardsTargetPlaceholder());
        AnimTimeInSeconds = MaxAnimTimeInSeconds - AnimTimeInSeconds;
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

    public void SelectRandomSong()
    {
        int songIndex = new System.Random().Next(0, songs.Count - 1);
        SelectSongByIndex(songIndex);
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
        if (IsDrag
            || IsFlickGesture)
        {
            return;
        }
        
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

    public void OnDrag(SongEntryControl songRouletteItem, Vector2 dragDeltaInPixels)
    {
        dragDuration += Time.deltaTime;
        SlotListControl.OnDrag(songRouletteItem, dragDeltaInPixels);
        songEntryControls.ForEach(it => SlotListControl.InterpolateSize(it));

        if (DragSongRouletteItem != null)
        {
            DragDistance = DragSongRouletteItem.GetPosition() - dragStartPosition;
        }
    }
    
    public void OnEndDrag(Vector2 dragDeltaInPixels)
    {
        CheckStartFlickGesture(dragDeltaInPixels);

        IsDrag = false;
        DragSongRouletteItem = null;
        DragDistance = Vector2.zero;
        songEntryControls.ForEach(it => it.StartAnimationTowardsTargetPlaceholder());
        ResetAnimationTimeTowardsTargetRouletteItem();

        songPreviewControl.StartSongPreview(Selection.Value);
    }

    public void OnBeginDrag(SongEntryControl songRouletteItem)
    {
        IsDrag = true;
        DragSongRouletteItem = songRouletteItem;
        songEntryControls.ForEach(it => it.StartAnimationTowardsTargetPlaceholder());
        songPreviewControl.StopSongPreview();

        // For velocity calculation
        dragStartPosition = songRouletteItem.GetPosition();
        dragDuration = 0;
    }
    
    private void CheckStartFlickGesture(Vector2 dragDeltaInPixels)
    {
        if (dragDeltaInPixels.magnitude > FlickGestureStartThresholdInPixels)
        {
            IsFlickGesture = true;
            flickGestureWasNoTouchscreenPressed = false;
            // Calculate final velocity of dragged element. The flick-gesture will continue with this velocity.
            dragDuration += Time.deltaTime;
            Vector2 finalRouletteItemPosition = DragSongRouletteItem.GetPosition() + dragDeltaInPixels;
            Vector2 finalDragDistance = finalRouletteItemPosition - dragStartPosition;
            dragVelocity = finalDragDistance / dragDuration;
        }
    }
    
    private void UpdateFlickGesture()
    {
        if (!IsFlickGesture)
        {
            return;
        }
        
        // Slowdown a little.
        dragVelocity *= 1 - (1 - FlickAccelerationPerSecond) * Time.deltaTime;
        if (dragVelocity.magnitude < FlickGestureStopThresholdInPixels
            || songEntryControls.IsNullOrEmpty()
            || InputUtils.AnyMouseButtonPressed()
            || (flickGestureWasNoTouchscreenPressed && InputUtils.AnyTouchscreenPressed()))
        {
            // End flick-gesture
            IsFlickGesture = false;
            dragVelocity = Vector2.zero;
            OnEndDrag(Vector2.zero);
            return;
        }

        // Stop flick when the touchscreen was pressed again (i.e., a rising flank).
        flickGestureWasNoTouchscreenPressed = flickGestureWasNoTouchscreenPressed || !InputUtils.AnyTouchscreenPressed();
        
        // Simulate drag in flick direction.
        SongEntryControl flickSongRouletteItem = songEntryControls.FirstOrDefault(it => it.SongMeta == SelectedSongMeta);
        OnDrag(flickSongRouletteItem, dragVelocity * Time.deltaTime);
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

    public void ShowSongMenuOverlay()
    {
        SelectedSongEntryControl.ShowSongMenuOverlay();
    }

    public void HideSongMenuOverlay()
    {
        SongEntryControls.ForEach(it => it.HideSongMenuOverlay());
    }

    public void ToggleSongMenuOverlay()
    {
        if (IsSongMenuOverlayVisible)
        {
            HideSongMenuOverlay();
        }
        else
        {
            ShowSongMenuOverlay();
        }
    }

    public void UpdateTranslation()
    {
        songEntryControls.ForEach(songEntryControl => songEntryControl.UpdateTranslation());
    }
}
