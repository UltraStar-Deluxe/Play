using System;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEntryControl : INeedInjection, IInjectionFinishedListener, IDisposable
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

    [Inject(UxmlName = R.UxmlNames.songEntryNotSavedYetIcon)]
    private VisualElement notSavedYetIcon;

    [Inject(UxmlName = R.UxmlNames.songEntryUiRoot)]
    private VisualElement songEntryUiRoot;

    [Inject(UxmlName = R.UxmlNames.openSongMenuButton)]
    private Button openSongMenuButton;

    [Inject]
    private CreateSingAlongSongControl createSingAlongSongControl;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;
    
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
            UpdateLabels();
            UpdateIcons();
            UpdateCover();
        }
    }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    private ContextMenuControl contextMenuControl;
    private bool isPopupMenuOpen;
    private float popupMenuClosedTimeInSeconds;

    private readonly Subject<bool> clickOnSongImageEventStream = new();
    public IObservable<bool> ClickOnSongImageEventStream => clickOnSongImageEventStream;

    private bool isInitialized;

    private readonly SongSelectSongRatingIconControl songRatingIconControl = new();

    private Vector2 pointerDownMousePosition;
    private bool wasSelectedOnPointerDown;
    
    public void OnInjectionFinished()
    {
        Init();
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        VisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        songImageOuter.RegisterCallback<PointerDownEvent>(OnPointerDownOnSongImage, TrickleDown.TrickleDown);
        songImageOuter.RegisterCallback<PointerUpEvent>(OnPointerUpOnSongImage, TrickleDown.TrickleDown);
        openSongMenuButton.RegisterCallbackButtonTriggered(OnOpenSongMenuButtonClicked);
    }

    private void UnregisterCallbacks()
    {
        VisualElement.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        songImageOuter.UnregisterCallback<PointerDownEvent>(OnPointerDownOnSongImage, TrickleDown.TrickleDown);
        songImageOuter.UnregisterCallback<PointerUpEvent>(OnPointerUpOnSongImage, TrickleDown.TrickleDown);
        openSongMenuButton.UnregisterCallbackButtonTriggered(OnOpenSongMenuButtonClicked);
    }

    private void Init()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        injector.Inject(songRatingIconControl);
        
        InitSongMenu();

        playlistManager.PlaylistChangeEventStream
            .Subscribe(evt => UpdateIcons());
        
        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(_ => UpdateIcons());
    }
    
    private void InitSongMenu()
    {
        contextMenuControl = injector
            .WithRootVisualElement(openSongMenuButton)
            .CreateAndInject<ContextMenuControl>();
        contextMenuControl.FillContextMenuAction = FillContextMenu;
        contextMenuControl.ContextMenuOpenedEventStream.Subscribe(OnContextMenuOpened);
        contextMenuControl.ContextMenuClosedEventStream.Subscribe(OnContextMenuClosed);
    }

    private void OnPointerDownOnSongImage(PointerDownEvent evt)
    {
        pointerDownMousePosition = evt.position;
        wasSelectedOnPointerDown = songRouletteControl.SelectedSongEntryControl == this;
    }
    
    private void OnPointerUpOnSongImage(PointerUpEvent evt)
    {
        if (evt.button == 0
            && Vector2.Distance(pointerDownMousePosition ,evt.position) < 5f
            && wasSelectedOnPointerDown)
        {
            clickOnSongImageEventStream.OnNext(true);
        }
    }
    
    private void OnPointerUp(PointerUpEvent evt)
    {
        // Open context menu on right click
        if (evt.button == 1
            && !isPopupMenuOpen
            && TimeUtils.IsDurationAboveThresholdInSeconds(popupMenuClosedTimeInSeconds, 0.1f))
        {
            contextMenuControl.OpenContextMenu(evt.position);
        }
    }

    private void OnOpenSongMenuButtonClicked(EventBase evt)
    {
        if (isPopupMenuOpen
            || !TimeUtils.IsDurationAboveThresholdInSeconds(popupMenuClosedTimeInSeconds, 0.1f))
        {
            return;
        }

        contextMenuControl.OpenContextMenu(new Vector2(-1, -1));
    }
    
    private void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        // Start song
        contextMenuPopup.AddButton("Start", "play_arrow",
            () =>
            {
                if (SongMeta != null)
                {
                    songSelectSceneControl.AttemptStartSong(SongMeta);
                }
            });
        
        // Add / remove from playlist
        playlistManager.Playlists
            .Where(playlist => playlist is UltraStarPlaylist
                               && playlist is not UltraStarAllSongsPlaylist)
            .ForEach(playlist =>
            {
                UltraStarPlaylist ultraStarPlaylist = playlist as UltraStarPlaylist;
                string playlistName = playlist.Name;
                if (playlistManager.HasSongEntry(playlist, songMeta))
                {
                    contextMenuPopup.AddButton($"Remove from '{playlistName}'", "favorite_border",
                        () => playlistManager.RemoveSongFromPlaylist(ultraStarPlaylist, songMeta));
                }
                else
                {
                    contextMenuPopup.AddButton($"Add to '{playlistName}'", "favorite",
                        () => playlistManager.AddSongToPlaylist(ultraStarPlaylist, songMeta));
                }
            });
        
        contextMenuPopup.AddButton("Enqueue", "playlist_add",
            () =>
            {
                if (SongMeta != null)
                {
                    songSelectSceneControl.AddSongToSongQueue(SongMeta);
                }
            });
        
        contextMenuPopup.AddButton("Enqueue Medley", "link",
            () =>
            {
                if (SongMeta != null)
                {
                    songSelectSceneControl.AddSongToSongQueueAsMedley(SongMeta);
                }
            });
        
        // Open song editor / song folder
        contextMenuPopup.AddButton("Open Editor", "edit",
            () => songSelectSceneControl.StartSongEditorScene());
        if (PlatformUtils.IsStandalone)
        {
            if (DirectoryUtils.Exists(SongMeta.Directory))
            {
                contextMenuPopup.AddButton("Open Folder", "open_in_new",
                    () => SongMetaUtils.OpenDirectory(SongMeta));
            }
            contextMenuPopup.AddButton("Reload Song", "replay",
                () => songMetaManager.ReloadSong(SongMeta));
        }

        contextMenuPopup.AddButton("Recreate Song", "replay_circle_filled",
        () => songSelectSceneControl.AskToRecreateSingAlongData(SongMeta));
        
        contextMenuPopup.AddButton("Info", "lyrics",
            () =>
            {
                if (SongMeta != null)
                {
                    songSelectSceneControl.ShowLyricsAndInfoPopup(SongMeta);
                }
            });

        VisualElement buttonContainer = contextMenuPopup.AddButton("Vocals Isolation", "call_split",
            () =>
            {
                if (SongMeta != null)
                {
                    audioSeparationManager.ProcessSongMeta(SongMeta);
                }
            });
        
        // Disable button if vocals and instrumental audio already exist.
        if (SongMetaUtils.VocalsAudioResourceExists(SongMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(SongMeta))
        {
            buttonContainer.Q<Button>().SetEnabled(false);
        }
    }

    private void OnContextMenuClosed(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = false;
        popupMenuClosedTimeInSeconds = Time.time;
    }
    
    private void OnContextMenuOpened(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = true;
        if (contextMenuPopupControl.Position.x < 0
            || contextMenuPopupControl.Position.y < 0)
        {
            new AnchoredPopupControl(contextMenuPopupControl.VisualElement, openSongMenuButton, Corner2D.BottomLeft);
        }
        contextMenuPopupControl.VisualElement.AddToClassList("singSceneContextMenu");
    }
    
    private void UpdateCover()
    {
        if (songMeta == null)
        {
            SetDefaultCoverImageWithColor();
            return;
        }
        
        SongMeta coverSongMeta = songMeta;
        string uri = SongMetaImageUtils.GetCoverOrBackgroundImageUri(coverSongMeta);
        if (uri.IsNullOrEmpty())
        {
            SetDefaultCoverImageWithColor();
            return;
        }
        
        ImageManager.LoadSpriteFromUri(uri,
            loadedSprite =>
            {
                if (coverSongMeta != songMeta)
                {
                    // The associated song has changed in the meantime.
                    return;
                }

                SetCoverImageWithoutColor(loadedSprite);
            },
            _ =>
            {
                if (coverSongMeta != songMeta)
                {
                    // The associated song has changed in the meantime.
                    return;
                }

                SetDefaultCoverImageWithColor();
            });
    }

    private void SetDefaultCoverImageWithColor()
    {
        SongMetaImageUtils.SetDefaultSongImage(songImageOuter, songImageInner);
        SongMetaImageUtils.SetDefaultSongImageColor(songMeta, songImageOuter, songImageInner);
    }
    
    private void SetCoverImageWithoutColor(Sprite sprite)
    {
        songImageOuter.style.backgroundImage = new StyleBackground(sprite);
        songImageOuter.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
        
        songImageInner.style.backgroundImage = new StyleBackground(sprite);
        songImageInner.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
    }

    private void UpdateIcons()
    {
        if (songMeta == null)
        {
            favoriteIcon.HideByDisplay();
            duetIcon.HideByDisplay();
            notSavedYetIcon.HideByDisplay();
            songRatingIconControl.HideSongRatingIcons();
            return;
        }
        
        favoriteIcon.SetVisibleByDisplay(playlistManager.FavoritesPlaylist.HasSongEntry(songMeta));
        duetIcon.SetVisibleByDisplay(songMeta.VoiceNames.Count > 1);
        notSavedYetIcon.SetVisibleByDisplay(SongMetaUtils.IsGeneratedAndNotYetSaved(songMeta));
        songRatingIconControl.UpdateSongRatingIcons(songMeta, settings.Difficulty);
    }
    
    private void UpdateLabels()
    {
        if (songMeta == null)
        {
            songArtist.text = "";
            songTitle.text = "";
            return;
        }
        
        songArtist.text = songMeta.Artist;
        songTitle.text = songMeta.Title;
    }

    public void Dispose()
    {
        SongMeta = null;
        UnregisterCallbacks();
    }

    public void OpenContextMenu()
    {
        if (!isPopupMenuOpen)
        {
            ContextMenuPopupControl contextMenuPopupControl = contextMenuControl?.OpenContextMenu(new Vector2(
                openSongMenuButton.worldBound.xMin,
                openSongMenuButton.worldBound.yMin));
            contextMenuPopupControl?.VisualElement.Q<Button>().Focus();
        }
    }
}
