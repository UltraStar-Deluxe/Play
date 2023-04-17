using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
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
    private Injector injector;

    public string Name { get; set; }

    public readonly Subject<bool> clickEventStream = new();
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
            UpdateCover();
        }
    }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    private ContextMenuControl contextMenuControl;
    private bool isPopupMenuOpen;
    private float popupMenuClosedTimeInSeconds;
    
    private bool isInitialized;

    public void OnInjectionFinished()
    {
        Init();
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        openSongMenuButton.RegisterCallbackButtonTriggered(OnOpenSongMenuButtonClicked);
    }

    private void UnregisterCallbacks()
    {
        openSongMenuButton.UnregisterCallbackButtonTriggered(OnOpenSongMenuButtonClicked);
    }

    private void Init()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        InitSongMenu();

        playlistManager.PlaylistChangeEventStream
            .Subscribe(evt => UpdateIcons());
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

    private void OnOpenSongMenuButtonClicked(EventBase evt)
    {
        if (isPopupMenuOpen
            || !TimeUtils.IsDurationAboveThreshold(popupMenuClosedTimeInSeconds, 0.1f))
        {
            return;
        }

        contextMenuControl.OpenContextMenu(Vector2.zero);
    }
    
    private void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
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

        // Open song editor / song folder
        contextMenuPopup.AddButton("Open Editor", "edit",
            () => songSelectSceneControl.StartSongEditorScene());
        if (PlatformUtils.IsStandalone)
        {
            contextMenuPopup.AddButton("Open Folder", "open_in_new",
                () => SongMetaUtils.OpenDirectory(SongMeta));
            contextMenuPopup.AddButton("Reload Song", "replay",
                () => songMetaManager.ReloadSong(SongMeta));
        }
        
        contextMenuPopup.AddButton("Recreate Song", "replay_circle_filled",
            () =>
            {
                if (SongMeta != null)
                {
                    createSingAlongSongControl.CreateSingAlongSong(SongMeta);
                }
            });
        
        contextMenuPopup.AddButton("Vocals Separation", "call_split",
            () =>
            {
                if (SongMeta != null)
                {
                    audioSeparationManager.ProcessSongMeta(SongMeta);
                }
            });
    }

    private void OnContextMenuClosed(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = false;
        popupMenuClosedTimeInSeconds = Time.time;
    }
    
    private void OnContextMenuOpened(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = true;
        new AnchoredPopupControl(contextMenuPopupControl.VisualElement, openSongMenuButton, Corner2D.BottomLeft);
        contextMenuPopupControl.VisualElement.AddToClassList("singSceneContextMenu");
    }
    
    private void UpdateCover()
    {
        SongMeta coverSongMeta = songMeta;
        string uri = SongMetaImageUtils.GetCoverOrBackgroundImageUri(coverSongMeta);
        if (uri.IsNullOrEmpty())
        {
            songImageOuter.style.backgroundImage = new StyleBackground();
            songImageInner.style.backgroundImage = new StyleBackground();
            return;
        }
        
        ImageManager.LoadSpriteFromUri(uri, loadedSprite =>
        {
            if (coverSongMeta != songMeta)
            {
                // The associated song has changed in the meantime.
                return;
            }
            songImageOuter.style.backgroundImage = new StyleBackground(loadedSprite);
            songImageInner.style.backgroundImage = new StyleBackground(loadedSprite);
        });
    }

    private void UpdateIcons()
    {
        favoriteIcon.SetVisibleByDisplay(playlistManager.FavoritesPlaylist.HasSongEntry(songMeta));
        duetIcon.SetVisibleByDisplay(songMeta.VoiceNames.Count > 1);
        notSavedYetIcon.SetVisibleByDisplay(SongMetaUtils.IsGeneratedAndNotYetSaved(songMeta));
    }

    public void Dispose()
    {
        UnregisterCallbacks();
    }
}
