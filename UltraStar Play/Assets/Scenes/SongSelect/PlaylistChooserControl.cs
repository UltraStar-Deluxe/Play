using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using ProTrans;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlaylistChooserControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.playlistChooser)]
    private DropdownField dropdownField;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Settings settings;

    private List<UltraStarPlaylist> items = new List<UltraStarPlaylist>();

    public ReactiveProperty<UltraStarPlaylist> Selection { get; private set; } = new ReactiveProperty<UltraStarPlaylist>();

    public void OnInjectionFinished()
    {
        InitDropdownChoices();

        // Update settings
        Selection.Subscribe(newPlaylist => settings.SongSelectSettings.playlistName = playlistManager.GetPlaylistName(newPlaylist));

        // Show playlist name in dropdown
        Selection.Subscribe(playlist =>
        {
            if (GetDisplayString(playlist) != dropdownField.value)
            {
                dropdownField.value = GetDisplayString(playlist);
            }
        });

        dropdownField.RegisterValueChangedCallback(evt =>
        {
            UltraStarPlaylist playlist = items.FirstOrDefault(it => GetDisplayString(it) == evt.newValue);
            Selection.Value = playlist;
        });

        playlistManager.PlaylistChangeEventStream
            .Subscribe(_ => InitDropdownChoices());
    }

    private void InitDropdownChoices()
    {
        items = new List<UltraStarPlaylist>();
        items.Add(new UltraStarAllSongsPlaylist());
        items.AddRange(playlistManager.Playlists);

        dropdownField.choices = items
            .Select(it => GetDisplayString(it))
            .ToList();

        // Initial selection
        UltraStarPlaylist newSelection = items
            .FirstOrDefault(playlist => GetDisplayString(playlist) == settings.SongSelectSettings.playlistName)
            .OrIfNull(items[0]);
        Selection.SetValueAndForceNotify(newSelection);
    }

    private string GetDisplayString(UltraStarPlaylist playlist)
    {
        if (playlist == null
            || playlist is UltraStarAllSongsPlaylist)
        {
            return TranslationManager.GetTranslation(R.Messages.filter_allSongs);
        }
        else
        {
            return playlistManager.GetPlaylistName(playlist);
        }
    }

    public void ToggleFavoritePlaylist()
    {
        if (items.IndexOf(Selection.Value) == 0)
        {
            Selection.Value = playlistManager.FavoritesPlaylist;
        }
        else
        {
            Selection.Value = items[0];
        }
    }

    public void Reset()
    {
        Selection.Value = items[0];
    }
}
