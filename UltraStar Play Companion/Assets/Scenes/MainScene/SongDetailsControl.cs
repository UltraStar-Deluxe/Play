using System;
using System.Collections.Generic;
using System.Linq;
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
    private Dictionary<string, string> voiceNameToLyricsMap = new();

    private readonly List<PlayerSelectPlayerEntryControl> playerEntryControls = new();

    private readonly GameRoundSettingsUiControl gameRoundSettingsUiControl = new();
    
    public void OnInjectionFinished()
    {
        injector.Inject(gameRoundSettingsUiControl);
        gameRoundSettingsUiControl.GameRoundSettings = settings.GameRoundSettings;

        gameRoundSettingsUiControl
            .DialogClosedEventStream
            .Subscribe(_ => enqueueSettingsAccordionItem.UpdateTargetHeight());

        mainGameHttpClient.Permissions.Subscribe(permissions => OnPermissionsChanged(permissions));
        
        HideSongDetails();
        backButton.RegisterCallbackButtonTriggered(() => HideSongDetails());
        favoriteButton.RegisterCallbackButtonTriggered(() => ToggleFavorite());
        enqueueButton.RegisterCallbackButtonTriggered(() => EnqueueSong());

        lyricsAccordionItem.ContentVisible = false;
        enqueueSettingsAccordionItem.ContentVisible = false;
    }

    private void OnPermissionsChanged(List<HttpApiPermission> permissions)
    {
        enqueueButton.SetVisibleByDisplay(permissions.Contains(HttpApiPermission.WriteSongQueue));
        enqueueSettingsAccordionItem.SetVisibleByDisplay(permissions.Contains(HttpApiPermission.WriteSongQueue));
    }
    
    private void EnqueueSong()
    {
        List<PlayerSelectPlayerEntryControl> selectedPlayerControls = GetSelectedPlayerControls();
        if (selectedPlayerControls.IsNullOrEmpty())
        {
            Debug.LogError("Cannot enqueue song. No player profiles selected.");
            UiManager.CreateNotification("Select a player first");
            return;
        }
        
        GameRoundDataDto dto = CreateGameRoundDataDto(selectedPlayerControls);
        string json = JsonConverter.ToJson(dto);
        mainGameHttpClient.PostRequest(HttpApiEndpointPaths.SongQueueEntry, json);
        
        HideSongDetails();
    }

    public List<PlayerSelectPlayerEntryControl> GetSelectedPlayerControls()
    {
        return playerEntryControls
            .Where(control => control.IsSelected)
            .ToList();
    }
    
    private GameRoundDataDto CreateGameRoundDataDto(List<PlayerSelectPlayerEntryControl> selectedPlayerControls)
    {
        GameRoundDataDto dto = new();
        dto.SongIds = new List<string>() { songDto.Hash };
        dto.SingScenePlayerDataDto = new SingScenePlayerDataDto();
        dto.SingScenePlayerDataDto.PlayerProfileNames = selectedPlayerControls
            .Select(control => control.PlayerProfileName)
            .ToList();

        dto.SingScenePlayerDataDto.PlayerProfileToMicProfileMap = new Dictionary<string, string>();
        selectedPlayerControls.ForEach(control =>
        {
            if (control.MicProfile != null)
            {
                dto.SingScenePlayerDataDto.PlayerProfileToMicProfileMap[control.PlayerProfileName] = control.MicProfile.Name;
            }
        });
        
        dto.SingScenePlayerDataDto.PlayerProfileToVoiceNameMap = new Dictionary<string, string>();
        selectedPlayerControls.ForEach(control =>
        {
            if (control.MicProfile != null)
            {
                dto.SingScenePlayerDataDto.PlayerProfileToVoiceNameMap[control.PlayerProfileName] = control.VoiceChooserControl.SelectedItem;
            }
        });

        dto.GameRoundSettings = settings.GameRoundSettings;
        return dto;
    }

    private void ToggleFavorite()
    {
        isFavorite = !isFavorite;
        if (isFavorite)
        {
            mainGameHttpClient.PostRequest(HttpApiEndpointPaths.PlaylistFavoritesEntry
                .ReplaceOrThrow("{songId}", songDto.Hash));
        }
        else
        {
            mainGameHttpClient.DeleteRequest(HttpApiEndpointPaths.PlaylistFavoritesEntry
                    .ReplaceOrThrow("{songId}", songDto.Hash));
        }
        UpdateFavoriteButton();
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
        
        LoadSongDetails();
        LoadSongImage();
        UpdateEnqueueSettings();
    }

    private void UpdateEnqueueSettings()
    {
        UpdatePlayersAndMics();
        UpdateEnqueueButton();
    }

    private void UpdatePlayersAndMics()
    {
        bool receivedPlayers = false;
        bool receivedMicrophones = false;
        List<string> playerProfileNames = null;
        List<MicProfile> micProfiles = null;

        playersContainer.Clear();
        playersContainer.Add(new Label("Loading players..."));
        enqueueSettingsAccordionItem.UpdateTargetHeight();
        
        playerEntryControls.Clear();

        mainGameHttpClient.GetRequest(HttpApiEndpointPaths.AvailablePlayers,
            response =>
            {
                ListDto<string> listDto = JsonConverter.FromJson<ListDto<string>>(response, false); 
                if (listDto == null
                    || listDto.Items == null)
                {
                    Debug.LogError($"Failed to get players. Response: {response}");
                    playersContainer.Clear();
                    playersContainer.Add(new Label("Failed to load players"));
                    return;
                }

                playerProfileNames = listDto.Items;
                
                receivedPlayers = true;
                if (receivedPlayers && receivedMicrophones)
                {
                    DoUpdatePlayersAndMics(playerProfileNames, micProfiles);
                }
            });
        
        mainGameHttpClient.GetRequest(HttpApiEndpointPaths.AvailableMicrophones,
            response =>
            {
                ListDto<MicProfile> listDto = JsonConverter.FromJson<ListDto<MicProfile>>(response, false); 
                if (listDto == null
                    || listDto.Items == null)
                {
                    Debug.LogError($"Failed to get available microphones. Response: {response}");
                    return;
                }

                if (listDto.Items.IsNullOrEmpty())
                {
                    Debug.LogWarning($"No available microphones found. Response: {response}");
                }

                micProfiles = listDto.Items;
                
                receivedMicrophones = true;
                if (receivedPlayers && receivedMicrophones)
                {
                    DoUpdatePlayersAndMics(playerProfileNames, micProfiles);
                }
            });
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
        List<MicProfile> unusedMicProfiles = micProfiles.ToList();

        void AssignUnusedMicProfile(PlayerSelectPlayerEntryControl playerEntryControl)
        {
            playerEntryControl.MicProfile = unusedMicProfiles.FirstOrDefault();
            if (playerEntryControl.MicProfile != null)
            {
                unusedMicProfiles.Remove(playerEntryControl.MicProfile);
            }
        }
        
        int playerProfileIndex = 0;
        playerProfileNames.ForEach(playerProfile =>
        {
            VisualElement playerEntry = playerSelectPlayerEntryUi.CloneTreeAndGetFirstChild();
            playersContainer.Add(playerEntry);

            PlayerSelectPlayerEntryControl playerEntryControl = injector
                .WithRootVisualElement(playerEntry)
                .WithBindingForInstance(playerProfile)
                .WithBinding(new Binding(nameof(micProfiles), new ExistingInstanceProvider<List<MicProfile>>(micProfiles)))
                .CreateAndInject<PlayerSelectPlayerEntryControl>();
            playerEntryControl.MicProfileChangedEventStream
                .Subscribe(evt =>
                {
                    // Deselect mic from other players.
                    playerEntryControls
                        .Where(it => it != evt.playerEntryControl && it.MicProfile == evt.newMicProfile)
                        .ForEach(it => it.MicProfile = null);
                });
            
            if (voiceNameToLyricsMap != null)
            {
                List<string> voiceNames = voiceNameToLyricsMap.Keys
                    .Select(voiceName => Voice.NormalizeVoiceName(voiceName))
                    .ToList();
                playerEntryControl.SetAvailableVoiceNames(voiceNames);
                if (!voiceNames.IsNullOrEmpty())
                {
                    playerEntryControl.VoiceChooserControl.SelectItem(voiceNames[playerProfileIndex % voiceNames.Count]);
                }
            }

            playerEntryControl.EnabledToggle.RegisterValueChangedCallback(evt =>
            {
                // Update mic profile.
                if (evt.newValue)
                {
                    AssignUnusedMicProfile(playerEntryControl);
                }
                else
                {
                    if (playerEntryControl.MicProfile != null)
                    {
                        unusedMicProfiles.Add(playerEntryControl.MicProfile);
                    }

                    playerEntryControl.MicProfile = null;
                }

                UpdateEnqueueButton();
            });

            playerEntryControls.Add(playerEntryControl);
            playerProfileIndex++;
        });

        UpdateEnqueueButton();
        enqueueSettingsAccordionItem.UpdateTargetHeight();
    }

    private void UpdateEnqueueButton()
    {
        enqueueButton.SetEnabled(!GetSelectedPlayerControls().IsNullOrEmpty());
    }

    private void LoadSongImage()
    {
        songImage.HideByVisibility();

        mainGameHttpClient.GetRequest(HttpApiEndpointPaths.SongImage.ReplaceOrThrow("{songId}", songDto.Hash),
            response =>
            {
                ImageDto imageDto = JsonConverter.FromJson<ImageDto>(response, false);
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
            });
    }

    private void LoadSongDetails()
    {
        SetLyrics("Loading lyrics...");

        mainGameHttpClient.GetRequest(HttpApiEndpointPaths.Song.ReplaceOrThrow("{songId}", songDto.Hash),
            response =>
            {
                SongDetailsDto songDetailsDto = JsonConverter.FromJson<SongDetailsDto>(response, false);
                if (songDetailsDto == null
                    || songDetailsDto.SongId.IsNullOrEmpty())
                {
                    Debug.LogError($"Failed to load details for song {songDto.Artist} - {songDto.Title}. Response: {response}");
                    return;
                }

                isFavorite = songDetailsDto.IsFavorite;
                UpdateLyrics(songDetailsDto.VoiceNameToLyricsMap);
                UpdateFavoriteButton();
            });
    }

    private void UpdateFavoriteButton()
    {
        favoriteIcon.SetVisibleByDisplay(isFavorite);
        noFavoriteIcon.SetVisibleByDisplay(!isFavorite);
    }

    private void UpdateLyrics(Dictionary<string,string> newVoiceNameToLyricsMap)
    {
        voiceNameToLyricsMap = newVoiceNameToLyricsMap;
        
        if (newVoiceNameToLyricsMap.Count <= 1)
        {
            SetLyrics(newVoiceNameToLyricsMap.Values.FirstOrDefault());
            return;
        }

        string GetVoiceDisplayName(string voiceName)
        {
            if (voiceName.IsNullOrEmpty()
                || voiceName == "P1")
            {
                return "Vocals 1";
            }
            else if (voiceName == "P2")
            {
                return "Vocals 2";
            }

            return voiceName;
        }
        
        string lyricsWithVoiceNames = newVoiceNameToLyricsMap
            .Select(entry => GetVoiceDisplayName(entry.Key) + ": " + entry.Value)
            .JoinWith("\n\n");
        SetLyrics(lyricsWithVoiceNames);
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
        songDetailsContainer.HideByDisplay();
        songListContainer.ShowByDisplay();
    }

    public void Dispose()
    {
        GameObject.Destroy(texture2D);
    }
}
