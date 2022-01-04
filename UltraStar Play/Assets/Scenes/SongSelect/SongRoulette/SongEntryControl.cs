using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using PrimeInputActions;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEntryControl : INeedInjection, IDragListener<GeneralDragEvent>, ISlotListItem, IInjectionFinishedListener
{
    // [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    // private SongRouletteItemContextMenuHandler songRouletteItemContextMenuHandler;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

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

    [Inject(UxmlName = R.UxmlNames.songMenuOverlay)]
    private VisualElement songOverlayMenu;

    [Inject(UxmlName = R.UxmlNames.modifyPlaylistButtonContainer)]
    private VisualElement modifyPlaylistButtonContainer;

    [Inject(UxmlName = R.UxmlNames.singThisSongButton)]
    private Button singThisSongButton;

    [Inject(UxmlName = R.UxmlNames.showSongDetailsButton)]
    private Button showSongDetailsButton;

    [Inject(UxmlName = R.UxmlNames.openSongEditorButton)]
    private Button openSongEditorButton;

    [Inject(UxmlName = R.UxmlNames.closeSongOverlayButton)]
    private Button closeSongOverlayButton;

    [Inject(UxmlName = R.UxmlNames.songPreviewVideoImage)]
    public VisualElement SongPreviewVideoImage { get; private set; }

    [Inject(UxmlName = R.UxmlNames.songPreviewBackgroundImage)]
    public VisualElement SongPreviewBackgroundImage { get; private set; }

    public Button Button => songButton;

    public string Name { get; set; }

    public bool IsSongMenuOverlayVisible => songOverlayMenu.IsVisibleByDisplay();

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
            UpdateFavoriteIcon();
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

    private Vector2 animStartPosition;
    private Vector2 animStartSize;
    
    public SongEntryControl(
        VisualElement visualElement,
        SongEntryPlaceholderControl targetPlaceholderControl,
        Vector2 initialPosition,
        Vector2 initialSize)
    {
        this.VisualElement = visualElement;
        this.targetPlaceholderControl = targetPlaceholderControl;

        // Initial size and position
        SetPosition(initialPosition);
        SetSize(initialSize);
        animStartPosition = initialPosition;
        animStartSize = initialSize;

        // Add itself as IDragListener to be notified when its RectTransform is dragged.
        // AddListener(this);
        // targetRectTransform = RectTransform;
    }

    public void Update()
    {
        // Destroy this item when it does not have a target.
        if (TargetPlaceholderControl == null)
        {
            songRouletteControl.RemoveSongRouletteItem(this);
            if (songRouletteControl.DragSongRouletteItem == this)
            {
                OnEndDrag(null);
            }
            VisualElement.RemoveFromHierarchy();
            return;
        }
        
        if (songRouletteControl.IsDrag
            || songRouletteControl.IsFlickGesture)
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
        float animPercent = songRouletteControl.AnimTimeInSeconds / songRouletteControl.MaxAnimTimeInSeconds;
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
        songRouletteControl.OnBeginDrag(this);
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        songRouletteControl.OnDrag(this, dragEvent.ScreenCoordinateInPixels.DragDelta);
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        songRouletteControl.OnEndDrag(dragEvent != null ? dragEvent.ScreenCoordinateInPixels.DragDelta : Vector2.zero);
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
        LeanTween.value(songRouletteControl.gameObject, Vector3.zero, Vector3.one, songRouletteControl.MaxAnimTimeInSeconds)
            .setOnUpdate((Vector3 value) => VisualElement.style.scale = new StyleScale(new Scale(value)));
    }

    public void OnInjectionFinished()
    {
        SongPreviewVideoImage.HideByDisplay();
        SongPreviewBackgroundImage.HideByDisplay();

        HideSongMenuOverlay();
        singThisSongButton.RegisterCallbackButtonTriggered(() =>
        {
            HideSongMenuOverlay();
            songSelectSceneUiControl.CheckAudioAndStartSingScene();
        });
        closeSongOverlayButton.RegisterCallbackButtonTriggered(() =>
        {
            HideSongMenuOverlay();
            InputManager.GetInputAction(R.InputActions.ui_submit).CancelNotifyForThisFrame();
        });
        openSongEditorButton.RegisterCallbackButtonTriggered(() => songSelectSceneUiControl.StartSongEditorScene());

        playlistManager.PlaylistChangeEventStream
            .Subscribe(evt => UpdateFavoriteIcon());
    }

    public void ShowSongMenuOverlay()
    {
        InitModifyPlaylistButtons();
        songOverlayMenu.ShowByDisplay();
        singThisSongButton.Focus();
    }

    private void InitModifyPlaylistButtons()
    {
        modifyPlaylistButtonContainer.Clear();
        playlistManager.Playlists
            .Where(playlist => !(playlist is UltraStarAllSongsPlaylist))
            .ForEach(playlist =>
        {
            string playlistName = playlistManager.GetPlaylistName(playlist);
            Button button = new Button();
            button.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            button.AddToClassList("smallFont");
            modifyPlaylistButtonContainer.Add(button);
            if (playlistManager.HasSongEntry(playlist, songMeta))
            {
                button.text = $"Remove from\n'{playlistName}'";
                button.RegisterCallbackButtonTriggered(() => playlistManager.RemoveSongFromPlaylist(playlist, songMeta));
            }
            else
            {
                button.text = $"Add to\n'{playlistName}'";
                button.RegisterCallbackButtonTriggered(() => playlistManager.AddSongToPlaylist(playlist, songMeta));
            }
        });
    }

    public void HideSongMenuOverlay()
    {
        songOverlayMenu.HideByDisplay();
    }

    private void UpdateFavoriteIcon()
    {
        favoriteIcon.SetVisibleByDisplay(playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title));
    }
}
