using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.IO;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongRouletteItemContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    public SongMeta SongMeta { get; set; }

    [Inject]
    private PlaylistManager playlistManager;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_reloadSong),
            () => SongMeta.Reload());

        if (PlatformUtils.IsStandalone)
        {
            contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_openSongFolder),
                () => SongMetaUtils.OpenDirectory(SongMeta));
            AddPlaylistContextMenuItems(contextMenu);
        }

        contextMenu.AddSeparator();
    }

    private void AddPlaylistContextMenuItems(ContextMenu contextMenu)
    {
        foreach (UltraStarPlaylist playlist in playlistManager.Playlists)
        {
            string playlistName = playlistManager.GetPlaylistName(playlist);
            Dictionary<string, string> placeholders = new Dictionary<string, string> { ["playlist"] = playlistName };
            if (playlist.HasSongEntry(SongMeta.Artist, SongMeta.Title))
            {
                contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_removeFromPlaylist, placeholders),
                    () => playlistManager.RemoveSongFromPlaylist(playlist, SongMeta));
            }
            else
            {
                contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_addToPlaylist, placeholders),
                    () => playlistManager.AddSongToPlaylist(playlist, SongMeta));
            }
        }
    }
}
