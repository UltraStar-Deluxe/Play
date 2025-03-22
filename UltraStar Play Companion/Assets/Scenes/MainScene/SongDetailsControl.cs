using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongDetailsControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject(Key = nameof(playerSelectPlayerEntryUi))]
    private VisualTreeAsset playerSelectPlayerEntryUi;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject(UxmlName = R.UxmlNames.songListContainer)]
    private VisualElement songListContainer;

    [Inject(UxmlName = R.UxmlNames.songDetailsContainer)]
    private VisualElement songDetailsContainer;

    [Inject(UxmlName = R.UxmlNames.songArtistLabel)]
    private Label songArtistLabel;

    [Inject(UxmlName = R.UxmlNames.songTitleLabel)]
    private Label songTitleLabel;

    [Inject(UxmlName = R.UxmlNames.songImage)]
    private VisualElement songImage;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.favoriteButton)]
    private Button favoriteButton;

    [Inject(UxmlName = R.UxmlNames.enqueueButton)]
    private Button enqueueButton;

    [Inject(UxmlName = R.UxmlNames.enqueueMedleyButton)]
    private Button enqueueMedleyButton;

    [Inject(UxmlName = R.UxmlNames.playersContainer)]
    private VisualElement playersContainer;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private VisualElement favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.noFavoriteIcon)]
    private VisualElement noFavoriteIcon;

    [Inject(UxmlName = R.UxmlNames.lyricsAccordionItem)]
    private AccordionItem lyricsAccordionItem;

    [Inject(UxmlName = R.UxmlNames.enqueueSettingsAccordionItem)]
    private AccordionItem enqueueSettingsAccordionItem;

    [Inject(UxmlName = R.UxmlNames.modifierDialogOverlay)]
    private VisualElement modifierDialogOverlay;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListView songListView;

    private SongDto songDto;
    public SongDto SongDto
    {
        get
        {
            return songDto;
        }
        set
        {
            songDto = value;
            UpdateControls();
        }
    }

    private bool isFavorite;

    private Texture2D texture2D;
    private Dictionary<string, string> voiceDisplayNameToLyricsMap = new();

    private readonly List<PlayerSelectPlayerEntryControl> playerEntryControls = new();

    private readonly GameRoundSettingsDtoUiControl gameRoundSettingsDtoUiControl = new();

    public void OnInjectionFinished()
    {
        injector.Inject(gameRoundSettingsDtoUiControl);

        gameRoundSettingsDtoUiControl.GameRoundSettingsDto = settings.GameRoundSettingsDto;
        VisualElementUtils.RegisterDirectClickCallback(modifierDialogOverlay, () => gameRoundSettingsDtoUiControl.CloseModifierDialog());

        gameRoundSettingsDtoUiControl
            .DialogClosedEventStream
            .Subscribe(_ => enqueueSettingsAccordionItem.UpdateTargetHeight());

        mainGameHttpClient.Permissions.Subscribe(permissions => OnPermissionsChanged(permissions));

        HideSongDetails();
        backButton.RegisterCallbackButtonTriggered(_ => HideSongDetails());
        favoriteButton.RegisterCallbackButtonTriggered(_ => ToggleFavorite());
        enqueueButton.RegisterCallbackButtonTriggered(_ => EnqueueSong());
        enqueueMedleyButton.RegisterCallbackButtonTriggered(_ => EnqueueSongAsMedley());

        lyricsAccordionItem.ContentVisible = false;
        enqueueSettingsAccordionItem.ContentVisible = false;
    }

    private void OnPermissionsChanged(List<RestApiPermission> permissions)
    {
        enqueueButton.SetVisibleByDisplay(permissions.Contains(RestApiPermission.WriteSongQueue));
        enqueueMedleyButton.SetVisibleByDisplay(enqueueButton.IsVisibleByDisplay());
        enqueueSettingsAccordionItem.SetVisibleByDisplay(permissions.Contains(RestApiPermission.WriteSongQueue));
    }

    private async void EnqueueSong()
    {
        List<PlayerSelectPlayerEntryControl> selectedPlayerControls = GetSelectedPlayerControls();
        if (selectedPlayerControls.IsNullOrEmpty())
        {
            Debug.LogError("Cannot enqueue song. No player profiles selected.");
            NotificationManager.CreateNotification(Translation.Of("Select a player first"));
            return;
        }

        HideSongDetails();
        NotificationManager.CreateNotification(Translation.Of($"Enqueued Song '{SongDto.Title}'"));

        SongQueueEntryDto dto = CreateSongQueueEntryDto(selectedPlayerControls, false);
        string json = JsonConverter.ToJson(dto);
        await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.SongQueueEntry, json);
    }

    private async void EnqueueSongAsMedley()
    {
        List<PlayerSelectPlayerEntryControl> selectedPlayerControls = GetSelectedPlayerControls();
        if (selectedPlayerControls.IsNullOrEmpty())
        {
            Debug.LogError("Cannot enqueue song. No player profiles selected.");
            NotificationManager.CreateNotification(Translation.Of("Select a player first"));
            return;
        }

        HideSongDetails();
        NotificationManager.CreateNotification(Translation.Of($"Enqueued Medley Song '{SongDto.Title}'"));

        SongQueueEntryDto dto = CreateSongQueueEntryDto(selectedPlayerControls, true);
        string json = JsonConverter.ToJson(dto);
        await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.SongQueueEntry, json);
    }

    public List<PlayerSelectPlayerEntryControl> GetSelectedPlayerControls()
    {
        return playerEntryControls
            .Where(control => control.IsSelected.Value)
            .ToList();
    }

    private SongQueueEntryDto CreateSongQueueEntryDto(List<PlayerSelectPlayerEntryControl> selectedPlayerControls, bool isMedleyWithPreviousEntry)
    {
        SongQueueEntryDto dto = new();
        dto.SongDto = songDto;
        dto.SingScenePlayerDataDto = new SingScenePlayerDataDto();
        dto.SingScenePlayerDataDto.PlayerProfileNames = selectedPlayerControls
            .Select(control => control.PlayerProfileName)
            .ToList();
        dto.IsMedleyWithPreviousEntry = isMedleyWithPreviousEntry;

        dto.SingScenePlayerDataDto.PlayerProfileToMicProfileMap = new Dictionary<string, MicProfileDto>();
        selectedPlayerControls.ForEach(control =>
        {
            if (control.MicProfile != null)
            {
                MicProfileDto micProfileDto = new()
                {
                    Name = control.MicProfile.Name,
                    ChannelIndex = control.MicProfile.ChannelIndex,
                    Color = control.MicProfile.Color,
                    Amplification = control.MicProfile.Amplification,
                    NoiseSuppression = control.MicProfile.NoiseSuppression,
                    SampleRate = control.MicProfile.SampleRate,
                    DelayInMillis = control.MicProfile.DelayInMillis,
                    IsEnabled = control.MicProfile.IsEnabled,
                    ConnectedClientId = control.MicProfile.ConnectedClientId,
                };
                dto.SingScenePlayerDataDto.PlayerProfileToMicProfileMap[control.PlayerProfileName] = micProfileDto;
            }
        });

        dto.SingScenePlayerDataDto.PlayerProfileToVoiceIdMap = new Dictionary<string, EExtendedVoiceId>();
        selectedPlayerControls.ForEach(control =>
        {
            if (control.MicProfile != null)
            {
                dto.SingScenePlayerDataDto.PlayerProfileToVoiceIdMap[control.PlayerProfileName] = control.VoiceChooserControl.Selection;
            }
        });

        dto.GameRoundSettingsDto = settings.GameRoundSettingsDto;
        return dto;
    }

    private async void ToggleFavorite()
    {
        isFavorite = !isFavorite;
        UpdateFavoriteButton();
        if (isFavorite)
        {
            await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.PlaylistFavoritesEntry
                .ReplaceOrThrow("{songId}", songDto.Hash));
        }
        else
        {
            await mainGameHttpClient.DeleteRequestAsync(RestApiEndpointPaths.PlaylistFavoritesEntry
                    .ReplaceOrThrow("{songId}", songDto.Hash));
        }
    }

    private void UpdateControls()
    {
        if (songDto == null)
        {
            return;
        }
        songArtistLabel.text = songDto.Artist;
        songTitleLabel.text = songDto.Title;

        enqueueButton.SetEnabled(false);
        enqueueMedleyButton.SetEnabled(enqueueButton.enabledInHierarchy);

        LoadSongDetails();
        LoadSongImage();
        UpdateEnqueueSettings();
    }

    private void UpdateEnqueueSettings()
    {
        UpdatePlayersAndMics();
        UpdateEnqueueButton();
    }

    private async void UpdatePlayersAndMics()
    {
        bool receivedPlayers = false;
        bool receivedMicrophones = false;
        List<string> playerProfileNames = null;
        List<MicProfile> micProfiles = null;

        playersContainer.Clear();
        playersContainer.Add(new Label("Loading players..."));
        enqueueSettingsAccordionItem.UpdateTargetHeight();

        playerEntryControls.Clear();

        playerProfileNames = await GetPlayerProfileNamesAsync();

        micProfiles = await GetMicProfilesAsync();

        DoUpdatePlayersAndMics(playerProfileNames, micProfiles);
    }

    private async Task<List<MicProfile>> GetMicProfilesAsync()
    {
        try
        {
            string response = await mainGameHttpClient.GetRequestAsync(RestApiEndpointPaths.AvailableMicrophones);
            ListDto<MicProfile> listDto = JsonConverter.FromJson<ListDto<MicProfile>>(response);
            if (listDto == null
                || listDto.Items == null)
            {
                Debug.LogError($"Failed to get available microphones. Response: {response}");
                return new List<MicProfile>();
            }

            if (listDto.Items.IsNullOrEmpty())
            {
                Debug.LogWarning($"No available microphones found. Response: {response}");
            }

            return listDto.Items;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return new List<MicProfile>();
    }

    private async Awaitable<List<string>> GetPlayerProfileNamesAsync()
    {
        try
        {
            string response = await mainGameHttpClient.GetRequestAsync(RestApiEndpointPaths.AvailablePlayers);
            ListDto<string> listDto = JsonConverter.FromJson<ListDto<string>>(response);
            if (listDto == null
                || listDto.Items == null)
            {
                Debug.LogError($"Failed to get players. Response: {response}");
                playersContainer.Clear();
                playersContainer.Add(new Label("Failed to load players"));
            }

            return listDto.Items;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return new List<string>();
    }

    private void DoUpdatePlayersAndMics(List<string> playerProfileNames, List<MicProfile> micProfiles)
    {
        if (playerProfileNames.IsNullOrEmpty())
        {
            Debug.LogError("Cannot update players and mics. PlayerProfiles are null or empty.");
            playersContainer.Clear();
            playersContainer.Add(new Label("No active players found.\nEdit players in the settings first."));
            return;
        }

        playersContainer.Clear();

        List<MicProfile> GetUnusedMicProfiles()
        {
            return micProfiles
                .Except(playerEntryControls.Select(playerEntryControl => playerEntryControl.MicProfile))
                .ToList();
        }

        void AssignUnusedMicProfile(PlayerSelectPlayerEntryControl playerEntryControl)
        {
            MicProfileReference lastUsedMicProfile = settings.PlayerProfileNameToLastUsedMicProfile
                .FirstOrDefault(entry => entry.Key == playerEntryControl.PlayerProfileName)
                .Value;

            List<MicProfile> unusedMicProfiles = GetUnusedMicProfiles();

            // Prefer the last used mic profile
            MicProfile unusedMicProfileThatWasUsedLastTime = unusedMicProfiles
                .FirstOrDefault(unusedMicProfile => Equals(new MicProfileReference(unusedMicProfile), lastUsedMicProfile));
            if (unusedMicProfileThatWasUsedLastTime != null)
            {
                playerEntryControl.MicProfile = unusedMicProfileThatWasUsedLastTime;
                return;
            }

            // Prefer a mic profile with the same name of the player
            MicProfile unusedMicProfileWithSameNameAsPlayer = unusedMicProfiles
                .FirstOrDefault(unusedMicProfile => string.Equals(unusedMicProfile.Name, playerEntryControl.PlayerProfileName, StringComparison.InvariantCultureIgnoreCase));
            if (unusedMicProfileWithSameNameAsPlayer != null)
            {
                playerEntryControl.MicProfile = unusedMicProfileWithSameNameAsPlayer;
                return;
            }

            // Use any unused mic profile, no further preferences.
            playerEntryControl.MicProfile = unusedMicProfiles.FirstOrDefault();
        }

        int playerProfileIndex = 0;
        playerProfileNames.ForEach(playerProfile =>
        {
            VisualElement playerEntry = playerSelectPlayerEntryUi.CloneTreeAndGetFirstChild();
            playersContainer.Add(playerEntry);

            PlayerSelectPlayerEntryControl playerEntryControl = injector
                .WithRootVisualElement(playerEntry)
                .WithBindingForInstance(playerProfile)
                .WithBinding(new UniInjectBinding(nameof(micProfiles), new ExistingInstanceProvider<List<MicProfile>>(micProfiles)))
                .CreateAndInject<PlayerSelectPlayerEntryControl>();
            playerEntryControl.OnMicProfileSelected = newMicProfile =>
            {
                // Deselect mic from other players.
                playerEntryControls
                    .Where(it => it.MicProfile == newMicProfile && it != playerEntryControl)
                    .ForEach(it => it.MicProfile = null);
            };

            List<EExtendedVoiceId> voiceIds = GetVoiceIdsFromVoiceDisplayNameToLyricsMap();
            playerEntryControl.SetAvailableVoiceIds(voiceIds);
            if (!voiceIds.IsNullOrEmpty())
            {
                playerEntryControl.VoiceChooserControl.Selection = voiceIds[playerProfileIndex % voiceIds.Count];
            }

            playerEntryControl.IsSelected.Value = IsPlayerSelectedInSettings(playerProfile);
            playerEntryControl.IsSelected.Subscribe(newValue =>
            {
                SetPlayerSelectedInSettings(playerProfile, newValue);

                // Update mic profile.
                if (newValue
                    && playerEntryControl.MicProfile == null)
                {
                    AssignUnusedMicProfile(playerEntryControl);
                }
                else if (!newValue
                         && playerEntryControl.MicProfile != null)
                {
                    playerEntryControl.MicProfile = null;
                }

                UpdateEnqueueButton();
            });

            playerEntryControl.SetSeparatorVisibleByDisplay(playerProfileIndex < playerProfileNames.Count - 1);

            playerEntryControls.Add(playerEntryControl);
            playerProfileIndex++;
        });

        UpdateEnqueueButton();
        enqueueSettingsAccordionItem.UpdateTargetHeight();
    }

    private List<EExtendedVoiceId> GetVoiceIdsFromVoiceDisplayNameToLyricsMap()
    {
        if (voiceDisplayNameToLyricsMap.IsNullOrEmpty())
        {
            return new();
        }

        if (voiceDisplayNameToLyricsMap.Count == 1)
        {
            return new List<EExtendedVoiceId>() { EExtendedVoiceId.P1, };
        }

        return new List<EExtendedVoiceId>() { EExtendedVoiceId.P1, EExtendedVoiceId.P2, EExtendedVoiceId.Merged };
    }

    private bool IsPlayerSelectedInSettings(string playerProfile)
    {
        return !settings.DeselectedPlayerProfiles.Contains(playerProfile);
    }

    private void SetPlayerSelectedInSettings(string playerProfile, bool selected)
    {
        if (selected)
        {
            settings.DeselectedPlayerProfiles.Remove(playerProfile);
        }
        else
        {
            settings.DeselectedPlayerProfiles.AddIfNotContains(playerProfile);
        }
    }

    private void UpdateEnqueueButton()
    {
        enqueueButton.SetEnabled(!GetSelectedPlayerControls().IsNullOrEmpty());
        enqueueMedleyButton.SetEnabled(enqueueButton.enabledInHierarchy);
    }

    private async void LoadSongImage()
    {
        songImage.HideByVisibility();

        try
        {
            string response = await mainGameHttpClient.GetRequestAsync(RestApiEndpointPaths.SongImage
                .ReplaceOrThrow("{songId}", songDto.Hash));

            ImageDto imageDto = JsonConverter.FromJson<ImageDto>(response);
            if (imageDto == null
                || imageDto.JpgBytesBase64.IsNullOrEmpty())
            {
                Debug.LogError($"Failed to load image for song {songDto.Artist} - {songDto.Title}. Response: {response}");
                return;
            }

            songImage.ShowByVisibility();
            byte[] jpgBytes = Convert.FromBase64String(imageDto.JpgBytesBase64);

            // Remove old texture if any
            GameObject.Destroy(texture2D);

            // Load new texture from bytes
            texture2D = new Texture2D(2, 2);
            // This will auto-resize the texture dimensions.
            texture2D.LoadImage(jpgBytes);

            songImage.style.backgroundImage = new StyleBackground(texture2D);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async void LoadSongDetails()
    {
        SetLyrics("Loading lyrics...");

        string response = await mainGameHttpClient.GetRequestAsync(RestApiEndpointPaths.Song
            .ReplaceOrThrow("{songId}", songDto.Hash));

        try
        {
            SongDetailsDto songDetailsDto = JsonConverter.FromJson<SongDetailsDto>(response);
            if (songDetailsDto == null
                || songDetailsDto.SongId.IsNullOrEmpty())
            {
                Debug.LogError($"Failed to load details for song {songDto.Artist} - {songDto.Title}. Response: {response}");
                return;
            }

            isFavorite = songDetailsDto.IsFavorite;
            UpdateLyrics(songDetailsDto.VoiceDisplayNameToLyricsMap);
            UpdateFavoriteButton();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void UpdateFavoriteButton()
    {
        favoriteIcon.SetVisibleByDisplay(isFavorite);
        noFavoriteIcon.SetVisibleByDisplay(!isFavorite);
    }

    private void UpdateLyrics(Dictionary<string, string> newVoiceDisplayNameToLyricsMap)
    {
        voiceDisplayNameToLyricsMap = newVoiceDisplayNameToLyricsMap;

        if (newVoiceDisplayNameToLyricsMap.Count <= 1)
        {
            SetLyrics(newVoiceDisplayNameToLyricsMap.Values.FirstOrDefault());
            return;
        }

        string GetVoiceDisplayName(EExtendedVoiceId voiceId)
        {
            switch (voiceId)
            {
                case EExtendedVoiceId.P1:
                    return "Vocals 1";
                case EExtendedVoiceId.P2:
                    return "Vocals 2";
                case EExtendedVoiceId.Merged:
                    return "Both";
                default:
                    return voiceId.ToString();
            }
        }

        try
        {
            string lyrics = "";
            if (newVoiceDisplayNameToLyricsMap.Count == 1)
            {
                lyrics = newVoiceDisplayNameToLyricsMap.FirstOrDefault().Value;
            }
            else if (newVoiceDisplayNameToLyricsMap.Count > 1)
            {
                lyrics = newVoiceDisplayNameToLyricsMap
                    .Select(entry =>
                    {
                        string voiceDisplayName = entry.Key;
                        string voiceLyrics = entry.Value;
                        return $"<i><b>{voiceDisplayName}</i></b>\n\n{voiceLyrics}";
                    })
                    .JoinWith("\n\n");
            }

            SetLyrics(lyrics);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to construct lyrics string");
            Debug.LogException(e);
            SetLyrics("");
        }
    }

    private void SetLyrics(string text)
    {
        lyricsAccordionItem.Clear();
        lyricsAccordionItem.Add(new Label(text));
        lyricsAccordionItem.UpdateTargetHeight();
    }

    public void ShowSongDetails(SongDto newSongDto)
    {
        SongDto = newSongDto;
        songDetailsContainer.ShowByDisplay();
        songListContainer.HideByDisplay();
    }

    public void HideSongDetails()
    {
        SaveLastUsedMicProfiles();

        songDetailsContainer.HideByDisplay();
        songListContainer.ShowByDisplay();
    }

    private void SaveLastUsedMicProfiles()
    {
        settings.PlayerProfileNameToLastUsedMicProfile.Clear();
        foreach (PlayerSelectPlayerEntryControl playerEntryControl in playerEntryControls)
        {
            if (playerEntryControl.MicProfile != null)
            {
                settings.PlayerProfileNameToLastUsedMicProfile[playerEntryControl.PlayerProfileName] = new MicProfileReference(playerEntryControl.MicProfile);
            }
        }
    }

    public void Dispose()
    {
        GameObject.Destroy(texture2D);
    }
}
