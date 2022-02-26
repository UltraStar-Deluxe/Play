using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEntryControl : INeedInjection, IDragListener<GeneralDragEvent>, ISlotListItem, IInjectionFinishedListener, ITranslator
{
    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

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

    [Inject(UxmlName = R.UxmlNames.songEntryFavoriteIcon)]
    private VisualElement favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.songEntryDuetIcon)]
    private VisualElement duetIcon;

    [Inject(UxmlName = R.UxmlNames.songButton)]
    private Button songButton;

    [Inject(UxmlName = R.UxmlNames.songMenuOverlay)]
    private VisualElement songOverlayMenu;

    [Inject(UxmlName = R.UxmlNames.modifyPlaylistButtonContainer)]
    private VisualElement modifyPlaylistButtonContainer;

    [Inject(UxmlName = R.UxmlNames.singThisSongButton)]
    private Button singThisSongButton;

    [Inject(UxmlName = R.UxmlNames.openSongEditorButton)]
    private Button openSongEditorButton;

    [Inject(UxmlName = R.UxmlNames.reloadSongButton)]
    private Button reloadSongButton;

    [Inject(UxmlName = R.UxmlNames.openSongFolderButton)]
    private Button openSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.closeSongOverlayButton)]
    private Button closeSongOverlayButton;

    [Inject(UxmlName = R.UxmlNames.songPreviewVideoImage)]
    public VisualElement SongPreviewVideoImage { get; private set; }

    [Inject(UxmlName = R.UxmlNames.songPreviewBackgroundImage)]
    public VisualElement SongPreviewBackgroundImage { get; private set; }

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    public string Name { get; set; }

    public bool IsSongMenuOverlayVisible => songOverlayMenu.IsVisibleByDisplay();

    public readonly Subject<bool> clickEventStream = new Subject<bool>();
    public IObservable<bool> ClickEventStream => clickEventStream;

    private bool ignoreNextClickEvent;

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
            songArtist.text = songMeta.Artist;
            songTitle.text = songMeta.Title;
            UpdateIcons();
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

    private GeneralDragControl dragControl;

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
        string coverUri = SongMetaUtils.GetCoverUri(coverSongMeta);
        if (coverUri.IsNullOrEmpty())
        {
            // Try the background image as fallback
            coverUri = SongMetaUtils.GetBackgroundUri(coverSongMeta);
            if (coverUri.IsNullOrEmpty())
            {
                return;
            }
        }

        if (!WebRequestUtils.ResourceExists(coverUri))
        {
            Debug.Log("Cover image resource does not exist: " + coverUri);
            return;
        }

        ImageManager.LoadSpriteFromUri(coverUri, loadedSprite =>
        {
            songImageOuter.style.backgroundImage = new StyleBackground(loadedSprite);
            songImageInner.style.backgroundImage = new StyleBackground(loadedSprite);
        });
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
        // Nothing to do. This method is part of IDragListener.
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
            songSelectSceneControl.CheckAudioAndStartSingScene();
        });
        closeSongOverlayButton.RegisterCallbackButtonTriggered(() =>
        {
            HideSongMenuOverlay();
            InputManager.GetInputAction(R.InputActions.ui_submit).CancelNotifyForThisFrame();
        });
        openSongEditorButton.RegisterCallbackButtonTriggered(() => songSelectSceneControl.StartSongEditorScene());
        if (PlatformUtils.IsStandalone)
        {
            openSongFolderButton.RegisterCallbackButtonTriggered(() => SongMetaUtils.OpenDirectory(SongMeta));
            reloadSongButton.RegisterCallbackButtonTriggered(() =>
            {
                SongMeta.Reload();
                HideSongMenuOverlay();
            });
        }
        else
        {
            openSongFolderButton.HideByDisplay();
            reloadSongButton.HideByDisplay();
        }

        playlistManager.PlaylistChangeEventStream
            .Subscribe(evt => UpdateIcons());

        // Add itself as IDragListener to be notified when its RectTransform is dragged.
        dragControl = injector
            .WithRootVisualElement(songButton)
            .CreateAndInject<GeneralDragControl>();
        dragControl.AddListener(this);

        // Ignore button click after dragging
        dragControl.DragState.Subscribe(dragState =>
        {
            if (dragState == EDragState.Dragging)
            {
                ignoreNextClickEvent = true;
            }
        });
        
        songButton.RegisterCallbackButtonTriggered(() =>
        {
            if (!ignoreNextClickEvent)
            {
                clickEventStream.OnNext(true);
            }
            ignoreNextClickEvent = false;
        });

        RegisterLongPressToOpenSongMenu();
        UpdateTranslation();
    }

    private void RegisterLongPressToOpenSongMenu()
    {
        Vector3 pointerDownEventPosition = Vector3.zero;
        IEnumerator showSongMenuOverlayCoroutine = null;

        void StopCoroutine()
        {
            if (showSongMenuOverlayCoroutine != null)
            {
                songRouletteControl.StopCoroutine(showSongMenuOverlayCoroutine);
                showSongMenuOverlayCoroutine = null;
            }
        }

        void StartCoroutine()
        {
            StopCoroutine();

            showSongMenuOverlayCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(1f, () =>
            {
                if (songRouletteControl.SelectedSongEntryControl != this)
                {
                    songRouletteControl.SelectSong(songMeta);
                }
                ignoreNextClickEvent = true;
                ShowSongMenuOverlay();
            });
            songRouletteControl.StartCoroutine(showSongMenuOverlayCoroutine);
        }

        songButton.RegisterCallback<PointerDownEvent>(evt =>
        {
            pointerDownEventPosition = evt.position;
            StartCoroutine();
        }, TrickleDown.TrickleDown);
        songButton.RegisterCallback<PointerUpEvent>(_ =>
        {
            StopCoroutine();
        }, TrickleDown.TrickleDown);

        // Stop coroutine when dragging
        dragControl.DragState.Subscribe(dragState =>
        {
            if (dragState == EDragState.Dragging)
            {
                StopCoroutine();
            }
        });
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

    private void UpdateIcons()
    {
        favoriteIcon.SetVisibleByDisplay(playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title));
        duetIcon.SetVisibleByDisplay(songMeta.VoiceNames.Count > 1);
    }

    public void FocusSongButton()
    {
        songButton.Focus();
    }

    public void UpdateTranslation()
    {
        singThisSongButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_songMenu_startButton);
        openSongFolderButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_songMenu_openSongFolder);
        openSongEditorButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_songMenu_openSongEditor);
        closeSongOverlayButton.text = TranslationManager.GetTranslation(R.Messages.back);
    }
}
