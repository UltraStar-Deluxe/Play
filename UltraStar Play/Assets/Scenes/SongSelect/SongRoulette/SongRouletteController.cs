using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniInject;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteController : MonoBehaviour, INeedInjection
{
    private const float FlickGestureThresholdInPixels = 20f;
    private const float FlickAcceleration = 0.98f;
    
    [InjectedInInspector]
    public RectTransform songRouletteItemContainer;
    
    [InjectedInInspector]
    public SongRouletteItem songRouletteItemPrefab;
    
    [InjectedInInspector]
    public List<RouletteItemPlaceholder> rouletteItemPlaceholders;

    [InjectedInInspector]
    public bool showRouletteItemPlaceholders;

    [Inject]
    private Injector injector;

    [Inject]
    private SongPreviewController songPreviewController;
    
    public SlotListControl SlotListControl { get; private set; } = new SlotListControl();

    private List<RouletteItemPlaceholder> activeRouletteItemPlaceholders = new List<RouletteItemPlaceholder>();
    private RouletteItemPlaceholder centerItem;
    private int centerItemIndex;

    private List<SongMeta> songs = new List<SongMeta>();

    public IReactiveProperty<SongSelection> Selection { get; private set; } = new ReactiveProperty<SongSelection>();

    private Subject<SongSelection> selectionClickedEventStream = new Subject<SongSelection>();
    public IObservable<SongSelection> SelectionClickedEventStream => selectionClickedEventStream;

    private readonly Dictionary<SongMeta, Button> songMetaToButtonMap = new Dictionary<SongMeta, Button>();

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
    
    private readonly List<SongRouletteItem> songRouletteItems = new List<SongRouletteItem>();

    private bool isInitialized;

    public bool IsDrag { get; private set; }
    public bool IsFlickGesture { get; private set; }
    public SongRouletteItem DragSongRouletteItem { get; private set; }
    
    public float MaxAnimTimeInSeconds { get; private set; } = 0.2f;
    public float AnimTimeInSeconds { get; set; }

    private bool flickGestureWasNoTouchscreenPressed;
    private Vector2 dragVelocity;
    private float dragDuration;
    private Vector2 dragStartPosition;
    
    void Start()
    {
        for (int i = 0; i < rouletteItemPlaceholders.Count; i++)
        {
            ISlotListSlot previousSlot = 0 <= (i - 1) ? rouletteItemPlaceholders[i - 1] : null;
            ISlotListSlot nextSlot = (i + 1) <= (rouletteItemPlaceholders.Count - 1) ? rouletteItemPlaceholders[i + 1] : null;
            rouletteItemPlaceholders[i].SetNeighborSlots(previousSlot, nextSlot);
        }
        SlotListControl.SlotChangeEventStream.Subscribe(OnSongRouletteItemChangedSlot);
        
        activeRouletteItemPlaceholders = new List<RouletteItemPlaceholder>(rouletteItemPlaceholders);
        centerItemIndex = (int)Math.Floor(rouletteItemPlaceholders.Count / 2f);
        centerItem = rouletteItemPlaceholders[centerItemIndex];

        if (!showRouletteItemPlaceholders)
        {
            foreach (RouletteItemPlaceholder item in rouletteItemPlaceholders)
            {
                Image image = item.GetComponentInChildren<Image>();
                image.enabled = false;
            }
        }
    }

    private void OnSongRouletteItemChangedSlot(SlotChangeEvent slotChangeEvent)
    {
        int newSelectedSongIndex = SelectedSongIndex;
        if (slotChangeEvent.Direction == ESlotListDirection.TowardsNextSlot)
        {
            newSelectedSongIndex = NumberUtils.Mod(SelectedSongIndex - 1, songs.Count);
            foreach (SongRouletteItem songRouletteItem in songRouletteItems)
            {
                RouletteItemPlaceholder nextPlaceholder = songRouletteItem.GetCurrentSlot().GetNextSlot() as RouletteItemPlaceholder;
                songRouletteItem.TargetRouletteItem = nextPlaceholder != null && activeRouletteItemPlaceholders.Contains(nextPlaceholder)
                    ? nextPlaceholder
                    : null;
            }
            
            int newlyCreatedSongIndex = NumberUtils.Mod(newSelectedSongIndex - (activeRouletteItemPlaceholders.Count / 2), songs.Count);
            CreateSongRouletteItem(songs[newlyCreatedSongIndex], activeRouletteItemPlaceholders.First());
        }
        else if (slotChangeEvent.Direction == ESlotListDirection.TowardsPreviousSlot)
        {
            newSelectedSongIndex = NumberUtils.Mod(SelectedSongIndex + 1, songs.Count);
            foreach (SongRouletteItem songRouletteItem in songRouletteItems)
            {
                RouletteItemPlaceholder previousPlaceholder = songRouletteItem.GetCurrentSlot().GetPreviousSlot() as RouletteItemPlaceholder;
                songRouletteItem.TargetRouletteItem = previousPlaceholder != null && activeRouletteItemPlaceholders.Contains(previousPlaceholder)
                    ? previousPlaceholder
                    : null;
            }

            int newlyCreatedSongIndex = NumberUtils.Mod(newSelectedSongIndex + (activeRouletteItemPlaceholders.Count / 2), songs.Count);
            CreateSongRouletteItem(songs[newlyCreatedSongIndex], activeRouletteItemPlaceholders.Last());
        }
        Selection.Value = new SongSelection(songs[newSelectedSongIndex] , newSelectedSongIndex, songs.Count);
    }

    void Update()
    {
        if (songs.Count == 0)
        {
            return;
        }

        if (!IsDrag
            && !IsFlickGesture)
        {
            UpdateSongRouletteItems();
        }
        
        AnimTimeInSeconds += Time.deltaTime;
        AnimTimeInSeconds = NumberUtils.Limit(AnimTimeInSeconds, 0, MaxAnimTimeInSeconds);

        UpdateDrawOrder();

        UpdateFlickGesture();
        
        // Initially, let all items start with full size
        if (!isInitialized)
        {
            isInitialized = true;
            foreach (SongRouletteItem songRouletteItem in songRouletteItems)
            {
                songRouletteItem.RectTransform.localScale = Vector3.one;
            }
        }
    }

    private void UpdateDrawOrder()
    {
        // Drawing order is defined by child order in the RectTransform.
        // Thus, sort children by distance to center.
        Vector2 centerPosition = centerItem.GetPosition();
        songRouletteItems.Sort((a, b) =>
        {
            float distanceA = Vector2.Distance(a.GetPosition(), centerPosition);
            float distanceB = Vector2.Distance(b.GetPosition(), centerPosition);
            return distanceB.CompareTo(distanceA);
        });
        for (int i = 0; i < songRouletteItems.Count; i++)
        {
            songRouletteItems[i].RectTransform.SetSiblingIndex(i);
        }
    }

    private void UpdateSongRouletteItems()
    {
        List<SongRouletteItem> usedSongRouletteItems = new List<SongRouletteItem>();

        // Spawn roulette items for songs to be displayed (i.e. selected song and its surrounding songs)
        int activeCenterIndex = activeRouletteItemPlaceholders.IndexOf(centerItem);
        foreach (RouletteItemPlaceholder placeholder in activeRouletteItemPlaceholders)
        {
            int index = activeRouletteItemPlaceholders.IndexOf(placeholder);
            int activeCenterDistance = index - activeCenterIndex;
            int songIndex = SelectedSongIndex + activeCenterDistance;
            SongMeta songMeta = GetSongAtIndex(songIndex);

            // Get or create SongRouletteItem for the song at the index
            SongRouletteItem songRouletteItem = songRouletteItems.Where(it => it.SongMeta == songMeta).FirstOrDefault();
            if (songRouletteItem == null)
            {
                songRouletteItem = CreateSongRouletteItem(songMeta, placeholder);
            }

            // Update target
            songRouletteItem.TargetRouletteItem = placeholder;
            usedSongRouletteItems.Add(songRouletteItem);
        }

        // Remove unused items
        foreach (SongRouletteItem songRouletteItem in new List<SongRouletteItem>(songRouletteItems))
        {
            if (!usedSongRouletteItems.Contains(songRouletteItem))
            {
                RemoveSongRouletteItem(songRouletteItem);
            }
        }
    }

    public void RemoveSongRouletteItem(SongRouletteItem songRouletteItem)
    {
        songRouletteItem.TargetRouletteItem = null;
        songRouletteItems.Remove(songRouletteItem);
        SlotListControl.ListItems.Remove(songRouletteItem);
    }

    private SongRouletteItem CreateSongRouletteItem(SongMeta songMeta, RouletteItemPlaceholder rouletteItem)
    {
        SongRouletteItem item = Instantiate(songRouletteItemPrefab, songRouletteItemContainer);
        injector.InjectAllComponentsInChildren(item);
        item.name = songMeta.Artist + "-" + songMeta.Title;
        item.SongMeta = songMeta;
        item.RectTransform.anchorMin = rouletteItem.RectTransform.anchorMin;
        item.RectTransform.anchorMax = rouletteItem.RectTransform.anchorMax;
        item.RectTransform.sizeDelta = Vector2.zero;
        item.RectTransform.anchoredPosition = Vector2.zero;
        item.TargetRouletteItem = rouletteItem;

        Button button = item.GetComponent<Button>();
        button.OnClickAsObservable().Subscribe(_ => OnSongButtonClicked(songMeta));
        songMetaToButtonMap[songMeta] = button;

        songRouletteItems.Add(item);
        SlotListControl.ListItems.Add(item);
        return item;
    }

    private void FindActiveRouletteItems()
    {
        List<int> activeRouletteItemIndexes = new List<int>();
        for (int i = 1; i <= songs.Count && i <= rouletteItemPlaceholders.Count; i++)
        {
            // Map the sequence (1,2,3,4,5,6,7,...), which is i,
            // to (0, -1, 1, -2, 2, -3, 3, ...), which is centerDistance.
            // This way, the available roulette items are added inside out.
            int centerDistance = ((int)Math.Floor(i / 2f)) * ((i % 2 == 0) ? -1 : 1);
            int availableItemIndex = centerItemIndex + centerDistance;
            activeRouletteItemIndexes.Add(availableItemIndex);
        }

        activeRouletteItemIndexes.Sort();
        activeRouletteItemPlaceholders.Clear();
        foreach (int index in activeRouletteItemIndexes)
        {
            if (index < rouletteItemPlaceholders.Count)
            {
                activeRouletteItemPlaceholders.Add(rouletteItemPlaceholders[index]);
            }
        }
    }

    private void UpdateRouletteItemsActiveState()
    {
        foreach (RouletteItemPlaceholder item in rouletteItemPlaceholders)
        {
            item.gameObject.SetActive(false);
        }

        foreach (RouletteItemPlaceholder item in activeRouletteItemPlaceholders)
        {
            item.gameObject.SetActive(true);
        }
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
            FindActiveRouletteItems();
            UpdateRouletteItemsActiveState();
        }
        else
        {
            Selection.Value = new SongSelection(null, -1, 0);
            RemoveAllSongRouletteItems();
        }
    }

    private int GetSongIndex(SongMeta songMeta)
    {
        return songs.IndexOf(songMeta);
    }

    private void RemoveAllSongRouletteItems()
    {
        foreach (SongRouletteItem songRouletteItem in songRouletteItems)
        {
            songRouletteItem.TargetRouletteItem = null;
        }
        songRouletteItems.Clear();
    }

    public void SelectSong(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        Selection.Value = new SongSelection(songMeta, songs.IndexOf(songMeta), songs.Count);
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

        ResetAnimationTimeTowardsTargetRouletteItem();
    }

    private void ResetAnimationTimeTowardsTargetRouletteItem()
    {
        // Animate towards target. Thereby, keep already finished part of previous animation.
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

    private SongMeta GetSongAtIndex(int nextIndex)
    {
        if (songs.Count == 0)
        {
            return null;
        }
        int wrappedIndex = (nextIndex < 0) ? nextIndex + songs.Count : nextIndex;
        int wrappedIndexModulo = wrappedIndex % songs.Count;
        SongMeta song = songs[wrappedIndexModulo];
        return song;
    }

    private void OnSongButtonClicked(SongMeta songMeta)
    {
        if (ContextMenu.IsAnyContextMenuOpen
            || IsDrag
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

    public void OnDrag(SongRouletteItem songRouletteItem, Vector2 dragDelta)
    {
        dragDuration += Time.deltaTime;
        SlotListControl.OnDrag(songRouletteItem, dragDelta);
        songRouletteItems.ForEach(it => SlotListControl.InterpolateSize(it));
    }
    
    public void OnEndDrag(Vector2 dragDeltaInPixels)
    {
        CheckStartFlickGesture(dragDeltaInPixels);
        
        IsDrag = false;
        DragSongRouletteItem = null;
        songRouletteItems.ForEach(it => it.StartAnimationTowardsTargetRouletteItem());
        ResetAnimationTimeTowardsTargetRouletteItem();
    }
    
    public void OnBeginDrag(SongRouletteItem songRouletteItem)
    {
        IsDrag = true;
        DragSongRouletteItem = songRouletteItem;
        songRouletteItems.ForEach(it => it.StartAnimationTowardsTargetRouletteItem());
        songPreviewController.StopSongPreview();
        
        // For velocity calculation
        dragStartPosition = songRouletteItem.GetPosition();
        dragDuration = 0;
    }
    
    private void CheckStartFlickGesture(Vector2 dragDeltaInPixels)
    {
        if (dragDeltaInPixels.magnitude > FlickGestureThresholdInPixels)
        {
            IsFlickGesture = true;
            flickGestureWasNoTouchscreenPressed = false;
            // Calculate final velocity of dragged element. The flick-gesture will continue with this velocity.
            dragDuration += Time.deltaTime;
            Vector2 finalPosition = DragSongRouletteItem.GetPosition() + dragDeltaInPixels;
            Vector2 dragDistance = finalPosition - dragStartPosition;
            dragVelocity = dragDistance / dragDuration;
        }
    }
    
    private void UpdateFlickGesture()
    {
        if (!IsFlickGesture)
        {
            return;
        }
        
        // Slowdown a little.
        dragVelocity *= FlickAcceleration;
        if ((dragVelocity.magnitude * Time.deltaTime) < FlickGestureThresholdInPixels
            || songRouletteItems.IsNullOrEmpty()
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
        SongRouletteItem flickSongRouletteItem = songRouletteItems.FirstOrDefault(it => it.SongMeta == SelectedSongMeta);
        OnDrag(flickSongRouletteItem, dragVelocity * Time.deltaTime);
    }
}
