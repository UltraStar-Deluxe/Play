using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteItem : GeneralDragHandler, INeedInjection, IDragListener<GeneralDragEvent>, ISlotListItem
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private SongRouletteItemContextMenuHandler songRouletteItemContextMenuHandler;

    [Inject]
    private SongRouletteController songRouletteController;
    
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
            songRouletteItemContextMenuHandler.SongMeta = songMeta;
            UpdateCover(songMeta);
        }
    }

    private RouletteItemPlaceholder targetRouletteItem;
    public RouletteItemPlaceholder TargetRouletteItem
    {
        get
        {
            return targetRouletteItem;
        }
        set
        {
            if (targetRouletteItem == value)
            {
                return;
            }
            targetRouletteItem = value;
            if (targetRouletteItem != null)
            {
                StartAnimationTowardsTargetRouletteItem();
            }
        }
    }

    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }

    private Vector3 animStartPosition;
    private Vector3 animStartSize;
    
    protected override void Start()
    {
        base.Start();
        // Animate to full scale
        if (RectTransform.localScale.magnitude < 0.1f)
        {
            LeanTween.scale(RectTransform, Vector3.one, 0.2f);
        }
        
        // Add itself as IDragListener to be notified when its RectTransform is dragged.
        AddListener(this);
        targetRectTransform = RectTransform;
    }

    public void Update()
    {
        // Destroy this item when it does not have a target.
        if (TargetRouletteItem == null)
        {
            songRouletteController.RemoveSongRouletteItem(this);
            if (songRouletteController.DragSongRouletteItem == this)
            {
                OnEndDrag(null);
            }
            Destroy(gameObject);
            return;
        }
        
        if (songRouletteController.IsDrag
            || songRouletteController.IsFlickGesture)
        {
            return;
        }
        UpdateMoveToTargetPosition(TargetRouletteItem.RectTransform.position, TargetRouletteItem.GetSize());
    }

    private void UpdateMoveToTargetPosition(Vector2 targetPosition, Vector2 targetSize)
    {
        float animPercent = songRouletteController.AnimTimeInSeconds / songRouletteController.MaxAnimTimeInSeconds;
        RectTransform.position = Vector3.Lerp(animStartPosition, targetPosition, animPercent);
        SetSize(Vector2.Lerp(animStartSize, targetSize, animPercent));
    }

    private void UpdateCover(SongMeta coverSongMeta)
    {
        Image image = GetComponent<Image>();
        string coverPath = coverSongMeta.Directory + Path.DirectorySeparatorChar + coverSongMeta.Cover;
        if (File.Exists(coverPath))
        {
            Sprite sprite = ImageManager.LoadSprite(coverPath);
            image.sprite = sprite;
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
        RectTransform.MoveAnchorsToCorners();
    }

    public bool IsCanceled()
    {
        return false;
    }

    public ISlotListSlot GetCurrentSlot()
    {
        return TargetRouletteItem;
    }

    public Vector2 GetPosition()
    {
        return RectTransform.position;
    }
    
    public void SetSize(Vector2 newSize)
    {
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);
    }

    public void SetPosition(Vector2 newPosition)
    {
        RectTransform.position = newPosition;
    }
    
    public void StartAnimationTowardsTargetRouletteItem()
    {
        animStartPosition = RectTransform.position;
        animStartSize = new Vector2(RectTransform.rect.width, RectTransform.rect.height);
    }
}
