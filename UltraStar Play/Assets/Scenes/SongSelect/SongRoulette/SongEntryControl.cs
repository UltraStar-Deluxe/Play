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

public class SongEntryControl : INeedInjection, IInjectionFinishedListener
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

    [Inject(UxmlName = R.UxmlNames.songEntryUiRoot)]
    private VisualElement songEntryUiRoot;

    [Inject(UxmlName = R.UxmlNames.openSongMenuButton)]
    private Button openSongMenuButton;

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
    
    public void OnInjectionFinished()
    {
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
        
        openSongMenuButton.RegisterCallbackButtonTriggered(() =>
        {
            if (isPopupMenuOpen
                || !TimeUtils.IsDurationAboveThreshold(popupMenuClosedTimeInSeconds, 0.1f))
            {
                return;
            }

            contextMenuControl.OpenContextMenu(Vector2.zero);
        });
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        // Add / remove from playlist
        playlistManager.Playlists
            .Where(playlist => !(playlist is UltraStarAllSongsPlaylist))
            .ForEach(playlist =>
            {
                string playlistName = playlist.Name;
                if (playlistManager.HasSongEntry(playlist, songMeta))
                {
                    contextMenuPopup.AddItem($"Remove from '{playlistName}'",
                        () => playlistManager.RemoveSongFromPlaylist(playlist, songMeta));
                }
                else
                {
                    contextMenuPopup.AddItem($"Add to '{playlistName}'",
                        () => playlistManager.AddSongToPlaylist(playlist, songMeta));
                }
            });

        // Open song editor / song folder
        contextMenuPopup.AddItem("Open Editor",
            () => songSelectSceneControl.StartSongEditorScene());
        if (PlatformUtils.IsStandalone)
        {
            contextMenuPopup.AddItem("Open Folder",
                () => SongMetaUtils.OpenDirectory(SongMeta));
            contextMenuPopup.AddItem("Reload Song",
                () => SongMeta.Reload());
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
        new AnchoredPopupControl(contextMenuPopupControl.VisualElement, openSongMenuButton, Corner2D.BottomLeft);
        contextMenuPopupControl.VisualElement.AddToClassList("singSceneContextMenu");
    }
    
    private void UpdateCover()
    {
        SongMetaImageUtils.SetCoverOrBackgroundImage(songMeta, songImageInner, songImageOuter);
    }

    private void UpdateIcons()
    {
        favoriteIcon.SetVisibleByDisplay(playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title));
        duetIcon.SetVisibleByDisplay(songMeta.VoiceNames.Count > 1);
    }
}
