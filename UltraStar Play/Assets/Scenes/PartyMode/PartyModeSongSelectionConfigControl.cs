using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeSongSelectionConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSettings partyModeSettings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.songSelectionChooser)]
    private Chooser songSelectionChooser;

    [Inject(UxmlName = R.UxmlNames.songSelectionPlaylistChooser)]
    private Chooser songSelectionPlaylistChooser;

    [Inject(UxmlName = R.UxmlNames.songSelectionJokerCountChooser)]
    private Chooser songSelectionJokerCountChooser;

    [Inject(UxmlName = R.UxmlNames.roundCountChooser)]
    private Chooser roundCountChooser;

    public void OnInjectionFinished()
    {
        // Round count
        NumberChooserControl roundCountChooserControl = new(roundCountChooser, 4);
        roundCountChooserControl.Bind(
            () => partyModeSettings.RoundCount,
            newValue => partyModeSettings.RoundCount = (int)newValue);

        // Selection mode (random or manual)
        EnumChooserControl<EPartyModeSongSelectionMode> songSelectionChooserControl = new(songSelectionChooser);
        songSelectionChooserControl.Bind(
            () => partyModeSettings.SongSelectionSettings.SongSelectionMode,
            newValue =>
            {
                UpdateControlsVisibility();
                partyModeSettings.SongSelectionSettings.SongSelectionMode = newValue;
            });

        // Playlist
        List<IPlaylist> playlists = playlistManager.GetPlaylists(true, true);
        LabeledChooserControl<IPlaylist> playlistChooserControl = new(songSelectionPlaylistChooser, playlists,
            newValue => Translation.Of(playlistManager.GetPlaylistName(newValue)));
        playlistChooserControl.Bind(
            () => partyModeSettings.SongSelectionSettings.SongPoolPlaylist,
            newValue => partyModeSettings.SongSelectionSettings.SongPoolPlaylist = newValue);

        // Joker count
        LabeledChooserControl<int> jokerCountChooserControl =
            new(songSelectionJokerCountChooser, new List<int> { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10},
                newValue => newValue >= 0 ? Translation.Of(newValue.ToString()) : Translation.Get(R.Messages.partyModeScene_jokerCount_unlimited));
        jokerCountChooserControl.Bind(
            () => partyModeSettings.SongSelectionSettings.JokerCount,
            newValue => partyModeSettings.SongSelectionSettings.JokerCount = newValue);

        // Only show the joker count for random song selection
        UpdateControlsVisibility();
        partyModeSettings.ObserveEveryValueChanged(it => it.SongSelectionSettings.SongSelectionMode)
            .Subscribe(_ => UpdateControlsVisibility());
    }

    private void UpdateControlsVisibility()
    {
        songSelectionJokerCountChooser.SetVisibleByDisplay(partyModeSettings.SongSelectionSettings.SongSelectionMode == EPartyModeSongSelectionMode.Random);
        songSelectionPlaylistChooser.SetVisibleByDisplay(partyModeSettings.SongSelectionSettings.SongSelectionMode == EPartyModeSongSelectionMode.Random);
    }
}
