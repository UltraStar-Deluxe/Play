using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

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

    [Inject(UxmlName = R.UxmlNames.songSelectionItemPicker)]
    private ItemPicker songSelectionItemPicker;

    [Inject(UxmlName = R.UxmlNames.songSelectionPlaylistItemPicker)]
    private ItemPicker songSelectionPlaylistItemPicker;

    [Inject(UxmlName = R.UxmlNames.songSelectionJokerCountItemPicker)]
    private ItemPicker songSelectionJokerCountItemPicker;

    public void OnInjectionFinished()
    {
        // Selection mode (random or manual)
        LabeledItemPickerControl<EPartyModeSongSelectionMode> songSelectionItemPickerControl =
            new(songSelectionItemPicker, EnumUtils.GetValuesAsList<EPartyModeSongSelectionMode>());
        songSelectionItemPickerControl.Bind(
            () => partyModeSettings.SongSelectionSettings.SongSelectionModeMode,
            newValue => partyModeSettings.SongSelectionSettings.SongSelectionModeMode = newValue);

        // Playlist
        List<UltraStarPlaylist> playlists = playlistManager.GetPlaylists(true, true);
        LabeledItemPickerControl<UltraStarPlaylist> playlistItemPickerControl = new(songSelectionPlaylistItemPicker, playlists);
        playlistItemPickerControl.GetLabelTextFunction = newValue => playlistManager.GetPlaylistName(newValue);
        playlistItemPickerControl.Bind(
            () => partyModeSettings.SongSelectionSettings.SongPoolPlaylist,
            newValue => partyModeSettings.SongSelectionSettings.SongPoolPlaylist = newValue);

        // Joker count
        LabeledItemPickerControl<int> jokerCountItemPickerControl =
            new(songSelectionJokerCountItemPicker, new List<int> { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
        jokerCountItemPickerControl.GetLabelTextFunction = newValue => newValue >= 0 ? newValue.ToString() : "Unlimited";
        jokerCountItemPickerControl.Bind(
            () => partyModeSettings.SongSelectionSettings.JokerCount,
            newValue => partyModeSettings.SongSelectionSettings.JokerCount = newValue);
    }
}
