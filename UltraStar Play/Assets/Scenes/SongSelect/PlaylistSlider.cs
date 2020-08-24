using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlaylistSlider : TextItemSlider<UltraStarPlaylist>, INeedInjection
{
    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Settings settings;

    protected override void Start()
    {
        base.Start();
        List<UltraStarPlaylist> playlists = new List<UltraStarPlaylist>();
        playlists.Add(new UltraStarAllSongsPlaylist());
        playlists.AddRange(playlistManager.Playlists);
        Items = playlists;
        Selection.Value = playlists
            .Where(playlist => playlistManager.GetPlaylistName(playlist) == settings.SongSelectSettings.playlistName)
            .FirstOrDefault()
            .OrIfNull(playlists[0]);
        Selection.Subscribe(newPlaylist => settings.SongSelectSettings.playlistName = playlistManager.GetPlaylistName(newPlaylist));
    }

    protected override string GetDisplayString(UltraStarPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist)
        {
            return "All Songs";
        }
        else
        {
            return playlistManager.GetPlaylistName(playlist);
        }
    }
}
