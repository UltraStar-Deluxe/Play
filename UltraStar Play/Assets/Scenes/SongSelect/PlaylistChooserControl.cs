using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlaylistChooserControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.playlistDropdownField)]
    private DropdownField playlistDropdownField;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Settings settings;

    private List<UltraStarPlaylist> items = new();

    public ReactiveProperty<UltraStarPlaylist> Selection { get; private set; } = new();

    public void OnInjectionFinished()
    {
        InitItems();

        // Update settings
        Selection.Subscribe(newPlaylist => settings.SongSelectSettings.playlistName = newPlaylist.Name);

        playlistDropdownField.choices = items
            .Select(playlist => GetDisplayString(playlist))
            .ToList();
        playlistDropdownField.value = GetDisplayString(items.FirstOrDefault());
        playlistDropdownField.RegisterValueChangedCallback(evt =>
        {
            UltraStarPlaylist playlist = items
                .FirstOrDefault(playlist => GetDisplayString(playlist) == evt.newValue);
            Selection.Value = playlist.OrIfNull(new UltraStarAllSongsPlaylist());
        });

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
            return playlist.Name;
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
        playlistDropdownField.Focus();
    }

    public void Reset()
    {
        Selection.Value = items[0];
    }
}
