using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEntryControl : INeedInjection, IDragListener<GeneralDragEvent>, ISlotListItem
{
    // [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    // private SongRouletteItemContextMenuHandler songRouletteItemContextMenuHandler;

    [Inject]
    private SongRouletteController songRouletteController;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.songImageOuter)]
    private VisualElement songImageOuter;

    [Inject(UxmlName = R.UxmlNames.songImageInner)]
    private VisualElement songImageInner;

    [Inject(UxmlName = R.UxmlNames.songArtist)]
    private Label songArtist;

    [Inject(UxmlName = R.UxmlNames.songTitle)]
    private Label songTitle;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private VisualElement favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.songButton)]
    private Button songButton;

    public Button Button => songButton;

    public string Name { get; set; }

    private SongMeta songMeta;
    public SongMeta SongMeta
    {
        get
        {
            return songMeta;
        }
        set
        {
            songMeta = value;
            // songRouletteItemContextMenuHandler.SongMeta = songMeta;
            songArtist.text = songMeta.Artist;
            songTitle.text = songMeta.Title;
            favoriteIcon.SetVisible(playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title));
            UpdateCover(songMeta);
        }
    }

    private SongEntryPlaceholderControl targetPlaceholderControl;
    public SongEntryPlaceholderControl TargetPlaceholderControl
    {
        get
        {
            return targetPlaceholderControl;
        }
        set
        {
            if (targetPlaceholderControl == value)
            {
                return;
            }
            targetPlaceholderControl = value;
            if (targetPlaceholderControl != null)
            {
                StartAnimationTowardsTargetPlaceholder();
            }
        }
    }

    public VisualElement VisualElement { get; private set; }

    private Vector3 animStartPosition;
    private Vector3 animStartSize;
    
    public SongEntryControl(VisualElement visualElement, SongEntryPlaceholderControl targetPlaceholderControl)
    {
        this.VisualElement = visualElement;
        this.targetPlaceholderControl = targetPlaceholderControl;

        // Add itself as IDragListener to be notified when its RectTransform is dragged.
        // AddListener(this);
        // targetRectTransform = RectTransform;
    }

    public void Update()
    {
        // Destroy this item when it does not have a target.
        if (TargetPlaceholderControl == null)
        {
            songRouletteController.RemoveSongRouletteItem(this);
            if (songRouletteController.DragSongRouletteItem == this)
            {
                OnEndDrag(null);
            }
            VisualElement.RemoveFromHierarchy();
            return;
        }
        
        if (songRouletteController.IsDrag
            || songRouletteController.IsFlickGesture)
        {
            return;
        }

        // Workaround for NaN in size value
        if (float.IsNaN(VisualElement.resolvedStyle.height)
            || float.IsNaN(VisualElement.resolvedStyle.width))
        {
            SetSize(TargetPlaceholderControl.GetSize());
        }

        UpdateMoveToTargetPosition(TargetPlaceholderControl.GetPosition(), TargetPlaceholderControl.GetSize());
    }

    private void UpdateMoveToTargetPosition(Vector2 targetPosition, Vector2 targetSize)
    {
        float animPercent = songRouletteController.AnimTimeInSeconds / songRouletteController.MaxAnimTimeInSeconds;
        Vector2 animatedPosition = Vector2.Lerp(animStartPosition, targetPosition, animPercent);
        SetPosition(animatedPosition);
        SetSize(Vector2.Lerp(animStartSize, targetSize, animPercent));
    }

    private void UpdateCover(SongMeta coverSongMeta)
    {
        string coverPath = coverSongMeta.Directory + Path.DirectorySeparatorChar + coverSongMeta.Cover;
        if (File.Exists(coverPath))
        {
            Sprite sprite = ImageManager.LoadSprite(coverPath);
            songImageOuter.style.backgroundImage = new StyleBackground(sprite);
            songImageInner.style.backgroundImage = new StyleBackground(sprite);
        }
        else
        {
            Debug.Log("Cover does not exist: " + coverPath);
        }
    }

    public void OnBeginDrag(GeneralDragEvent dragEvent)
    {
        songRouletteController.OnBeginDrag(this);
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        songRouletteController.OnDrag(this, dragEvent.ScreenCoordinateInPixels.DragDelta);
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        songRouletteController.OnEndDrag(dragEvent != null ? dragEvent.ScreenCoordinateInPixels.DragDelta : Vector2.zero);
    }

    public void CancelDrag()
    {
        // RectTransform.MoveAnchorsToCorners();
    }

    public bool IsCanceled()
    {
        return false;
    }

    public ISlotListSlot GetCurrentSlot()
    {
        return TargetPlaceholderControl;
    }

    public Vector2 GetPosition()
    {
        return new Vector2(VisualElement.resolvedStyle.left, VisualElement.resolvedStyle.top);
    }
    
    public void SetSize(Vector2 newSize)
    {
        VisualElement.style.width = newSize.x;
        VisualElement.style.height = newSize.y;
    }

    public Vector2 GetSize()
    {
        return VisualElement.contentRect.size;
    }

    public void SetPosition(Vector2 newPosition)
    {
        VisualElement.style.left = newPosition.x;
        VisualElement.style.top = newPosition.y;
    }
    
    public void StartAnimationTowardsTargetPlaceholder()
    {
        animStartPosition = GetPosition();
        animStartSize = GetSize();
    }

    public void StartAnimationToFullScale()
    {
        VisualElement.style.scale = new StyleScale(new Scale(Vector3.zero));
        LeanTween.value(songRouletteController.gameObject, Vector3.zero, Vector3.one, 0.2f)
            .setOnUpdate((Vector3 value) => VisualElement.style.scale = new StyleScale(new Scale(value)));
    }
}
