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
        if (PlatformUtils.IsStandalone)
        {
            contextMenu.AddItem("Open song folder", () => SongMetaUtils.OpenDirectory(SongMeta));
            contextMenu.AddSeparator();
            AddPlaylistContextMenuItems(contextMenu);
        }
    }

    private void AddPlaylistContextMenuItems(ContextMenu contextMenu)
    {
        foreach (UltraStarPlaylist playlist in playlistManager.Playlists)
        {
            string playlistName = playlistManager.GetPlaylistName(playlist);
            if (playlist.HasSongEntry(SongMeta.Artist, SongMeta.Title))
            {
                contextMenu.AddItem($"Remove from '{playlistName}'", () => playlistManager.RemoveSongFromPlaylist(playlist, SongMeta));
            }
            else
            {
                contextMenu.AddItem($"Add to '{playlistName}'", () => playlistManager.AddSongToPlaylist(playlist, SongMeta));
            }
        }
    }
}
