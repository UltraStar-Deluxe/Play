using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectionPlaylistChooserControl : INeedInjection, IInjectionFinishedListener, ITranslator
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

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    private List<IPlaylist> items = new();

    public ReactiveProperty<IPlaylist> Selection { get; private set; } = new();

    public bool IsPlaylistChooserDropdownOverlayVisible => playlistChooserDropdownOverlay.IsVisibleByDisplay();

    public void OnInjectionFinished()
    {
        InitItems();

        // Update settings
        Selection.Subscribe(newPlaylist => settings.SongSelectSettings.playlistName = newPlaylist.Name);

        // Show playlist name in button
        Selection.Subscribe(playlist => playlistChooserButton.text = playlistManager.GetPlaylistName(playlist));

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
        items = new List<IPlaylist>();
        items.Add(UltraStarAllSongsPlaylist.Instance);
        items.Add(playlistManager.FavoritesPlaylist);
        items.AddRange(playlistManager.Playlists.Where(playlist => playlist != playlistManager.FavoritesPlaylist));

        // Initial selection
        IPlaylist newSelection;
        if (songSelectSceneControl.UsePartyModePlaylist)
        {
            newSelection = songSelectSceneControl.PartyModeSettings.songSelectionSettings.songPoolPlaylist;
        }
        else
        {
            // Use last selected playlist or the first
            newSelection = items
                .FirstOrDefault(playlist => playlistManager.GetPlaylistName(playlist) == settings.SongSelectSettings.playlistName)
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
        playlistChooserButton.Focus();
    }

    public void Reset()
    {
        if (songSelectSceneControl.UsePartyModePlaylist)
        {
            return;
        }

        Selection.Value = items[0];
    }

    public void HidePlaylistChooserDropdownOverlay()
    {
        playlistChooserDropdownOverlay.HideByDisplay();
    }

    public void ShowPlaylistChooserDropdownOverlay()
    {
        if (songSelectSceneControl.UsePartyModePlaylist)
        {
            // Changing the playlist is not allowed
            UiManager.CreateNotification("Using playlist from party mode settings");
            return;
        }

        playlistChooserDropdownOverlay.ShowByDisplay();

        // Fill dropdown with playlist buttons
        playlistChooserDropdownScrollView.Clear();
        items.ForEach(item => playlistChooserDropdownScrollView.Add(CreatePlaylistButton(item)));

        // Focus first button
        playlistChooserDropdownScrollView.Children()
            .FirstOrDefault()
            .IfNotNull(child => child.Focus());
    }

    private Button CreatePlaylistButton(IPlaylist item)
    {
        Button button = new();
        button.text = playlistManager.GetPlaylistName(item);
        button.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

        button.RegisterCallbackButtonTriggered(() =>
        {
            if (songSelectSceneControl.UsePartyModePlaylist)
            {
                return;
            }

            Selection.Value = item;
            HidePlaylistChooserDropdownOverlay();
        });
        return button;
    }

    public void UpdateTranslation()
    {
        playlistChooserButton.text = playlistManager.GetPlaylistName(Selection.Value);
    }
}
