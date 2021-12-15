using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.IO;
using UnityEngine.InputSystem;
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteItemContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    public SongMeta SongMeta { get; set; }

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

    [Inject]
    private SongRouletteController songRouletteController;
    
    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_reloadSong),
            () => SongMeta.Reload());

        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            () => songSelectSceneUiControl.StartSongEditorScene());

        if (PlatformUtils.IsStandalone)
        {
            contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_openSongFolder),
                () => SongMetaUtils.OpenDirectory(SongMeta));
            AddPlaylistContextMenuItems(contextMenu);
        }

        contextMenu.AddSeparator();
    }

    protected override void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (songRouletteController.DragDistance.magnitude > DragDistanceThreshold
            || songRouletteController.IsFlickGesture)
        {
            // Do not open when drag-gesture is in progress.
            return;
        }
        base.CheckOpenContextMenuFromInputAction(context);
    }

    private void AddPlaylistContextMenuItems(ContextMenu contextMenu)
    {
        foreach (UltraStarPlaylist playlist in playlistManager.Playlists)
        {
            string playlistName = playlistManager.GetPlaylistName(playlist);
            Dictionary<string, string> placeholders = new Dictionary<string, string> { ["playlist"] = playlistName };
            if (playlist.HasSongEntry(SongMeta.Artist, SongMeta.Title))
            {
                contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_removeFromPlaylist, placeholders),
                    () => playlistManager.RemoveSongFromPlaylist(playlist, SongMeta));
            }
            else
            {
                contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_addToPlaylist, placeholders),
                    () => playlistManager.AddSongToPlaylist(playlist, SongMeta));
            }
        }
    }
}
