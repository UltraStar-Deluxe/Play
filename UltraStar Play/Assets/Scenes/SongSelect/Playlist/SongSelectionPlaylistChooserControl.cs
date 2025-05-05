using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectionPlaylistChooserControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.playlistDropdownField)]
    private DropdownField playlistDropdownField;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private GameObject gameObject;

    private List<IPlaylist> items = new();

    public ReactiveProperty<IPlaylist> Selection { get; private set; } = new();

    public void OnInjectionFinished()
    {
        UpdateItems();

        // Update settings
        Selection.Subscribe(newPlaylist => nonPersistentSettings.PlaylistName.Value = newPlaylist.Name);

        playlistDropdownField.value = items.FirstOrDefault().Name;
        playlistDropdownField.RegisterValueChangedCallback(evt =>
        {
            IPlaylist playlist = items
                .FirstOrDefault(playlist => playlist.Name == evt.newValue);
            Selection.Value = playlist.OrIfNull(new UltraStarAllSongsPlaylist());
        });

        nonPersistentSettings.PlaylistName
            .Subscribe(newPlaylistName =>
            {
                if (playlistDropdownField.value != newPlaylistName)
                {
                    playlistDropdownField.value = newPlaylistName;
                }
            });

        playlistManager.PlaylistChangedEventStream
            .Subscribe(_ => UpdateItems())
            .AddTo(gameObject);

        playlistManager.PlaylistsLoadedEventStream
            .Subscribe(_ => UpdateItems())
            .AddTo(gameObject);

    }

    private void UpdateItems()
    {
        items = playlistManager.GetPlaylists(true, true)
            .OrderBy(playlist => playlist.Name)
            .ToList();

        playlistDropdownField.choices = items
            .Select(playlist => playlist.Name)
            .ToList();

        // Initial selection
        IPlaylist newSelection;
        if (songSelectSceneControl.UsePartyModePlaylist)
        {
            newSelection = songSelectSceneControl.PartyModeSettings.SongSelectionSettings.SongPoolPlaylist;
        }
        else
        {
            // Use last selected playlist or the first
            newSelection = items
                .FirstOrDefault(playlist => playlistManager.GetPlaylistName(playlist) == nonPersistentSettings.PlaylistName.Value)
                .OrIfNull(items.FirstOrDefault());
        }
        Selection.SetValueAndForceNotify(newSelection);
    }

    public void ToggleFavoritePlaylist()
    {
        if (songSelectSceneControl.UsePartyModePlaylist)
        {
            return;
        }

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
        playlistDropdownField.Focus();
    }

    public void Reset()
    {
        if (songSelectSceneControl.UsePartyModePlaylist)
        {
            return;
        }

        Selection.Value = items[0];
    }
}
