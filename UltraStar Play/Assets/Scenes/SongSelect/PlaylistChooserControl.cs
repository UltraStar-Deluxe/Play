﻿using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlaylistChooserControl : INeedInjection, IInjectionFinishedListener, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.playlistChooserButton)]
    private Button playlistChooserButton;

    [Inject(UxmlName = R.UxmlNames.closePlaylistChooserDropdownButton)]
    private Button closePlaylistChooserDropdownButton;

    [Inject(UxmlName = R.UxmlNames.playlistChooserDropdownOverlay)]
    private VisualElement playlistChooserDropdownOverlay;

    [Inject(UxmlName = R.UxmlNames.playlistChooserDropdownScrollView)]
    private ScrollView playlistChooserDropdownScrollView;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Settings settings;

    private List<UltraStarPlaylist> items = new();

    public ReactiveProperty<UltraStarPlaylist> Selection { get; private set; } = new();

    public bool IsPlaylistChooserDropdownOverlayVisible => playlistChooserDropdownOverlay.IsVisibleByDisplay();

    public void OnInjectionFinished()
    {

        InitItems();

        // Update settings
        Selection.Subscribe(newPlaylist => settings.SongSelectSettings.playlistName = playlistManager.GetPlaylistName(newPlaylist));

        // Show playlist name in button
        Selection.Subscribe(playlist => playlistChooserButton.text = GetDisplayString(playlist));

        HidePlaylistChooserDropdownOverlay();
        playlistChooserButton.RegisterCallbackButtonTriggered(() =>
        {
            if (IsPlaylistChooserDropdownOverlayVisible)
            {
                HidePlaylistChooserDropdownOverlay();
            }
            else
            {
                ShowPlaylistChooserDropdownOverlay();
            }
        });
        closePlaylistChooserDropdownButton.RegisterCallbackButtonTriggered(() => HidePlaylistChooserDropdownOverlay());

        playlistManager.PlaylistChangeEventStream
            .Subscribe(_ => InitItems());
    }

    private void InitItems()
    {
        items = new List<UltraStarPlaylist>();
        items.Add(new UltraStarAllSongsPlaylist());
        items.Add(playlistManager.FavoritesPlaylist);
        items.AddRange(playlistManager.Playlists.Where(playlist => playlist != playlistManager.FavoritesPlaylist));

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
            return TranslationManager.GetTranslation(R.Messages.playlistName_allSongs);
        }
        else if (playlist == playlistManager.FavoritesPlaylist)
        {
            return TranslationManager.GetTranslation(R.Messages.playlistName_favorites);
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

    public void FocusPlaylistChooser()
    {
        playlistChooserButton.Focus();
    }

    public void Reset()
    {
        Selection.Value = items[0];
    }

    public void HidePlaylistChooserDropdownOverlay()
    {
        playlistChooserDropdownOverlay.HideByDisplay();
    }

    public void ShowPlaylistChooserDropdownOverlay()
    {
        playlistChooserDropdownOverlay.ShowByDisplay();

        // Fill dropdown with playlist buttons
        playlistChooserDropdownScrollView.Clear();
        items.ForEach(item => playlistChooserDropdownScrollView.Add(CreatePlaylistButton(item)));

        // Focus first button
        playlistChooserDropdownScrollView.Children()
            .FirstOrDefault()
            .IfNotNull(child => child.Focus());
    }

    private Button CreatePlaylistButton(UltraStarPlaylist item)
    {
        Button button = new();
        button.text = GetDisplayString(item);
        button.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

        button.RegisterCallbackButtonTriggered(() =>
        {
            Selection.Value = item;
            HidePlaylistChooserDropdownOverlay();
        });
        return button;
    }

    public void UpdateTranslation()
    {
        playlistChooserButton.text = GetDisplayString(Selection.Value);
    }
}
