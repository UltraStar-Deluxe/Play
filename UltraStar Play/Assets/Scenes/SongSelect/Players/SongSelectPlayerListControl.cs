using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectPlayerListControl : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public VisualTreeAsset playerEntryUi;

    [Inject(UxmlName = R.UxmlNames.playerList)]
    private VisualElement playerList;

    private readonly List<SongSelectPlayerEntryControl> playerEntryControls = new();
    public IReadOnlyList<SongSelectPlayerEntryControl> PlayerEntryControls => playerEntryControls;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private Injector injector;

    [Inject]
    private SongRouletteControl songRouletteControl;

    void Start()
    {
        UpdateListEntries();
        LoadLastPlayerProfileToMicProfileMap();

        // Remove/add MicProfile when Client (dis)connects.
        serverSideCompanionClientManager.ClientConnectionChangedEventStream
            .ObserveOnMainThread()
            .Subscribe(OnClientConnectionChanged)
            .AddTo(gameObject);

        micSampleRecorderManager.ConnectedMicDevicesChangesStream
            .Subscribe(OnConnectedMicDevicesChanged)
            .AddTo(gameObject);

        if (songSelectSceneControl.HasPartyModeSceneData)
        {
            SelectMicsForPartyMode();
        }
    }

    private void Update()
    {
        playerEntryControls.ForEach(it => it.Update());
    }

    public SingScenePlayerData CreateSingScenePlayerData()
    {
        SingScenePlayerData singScenePlayerData = new();

        List<PlayerProfile> selectedPlayerProfiles = GetSelectedPlayerProfiles();
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_noPlayerSelected_title));
            return null;
        }
        singScenePlayerData.SelectedPlayerProfiles = selectedPlayerProfiles;
        singScenePlayerData.PlayerProfileToMicProfileMap = GetSelectedPlayerProfileToMicProfileMap();
        singScenePlayerData.PlayerProfileToVoiceIdMap = GetSelectedPlayerProfileToExtendedVoiceIdMap();
        return singScenePlayerData;
    }

    private void SelectMicsForPartyMode()
    {
        // Assign mics by re-selecting every player profile of this round.
        // TODO: Prefer same mic of the team from last round
        playerEntryControls.ForEach(playerEntryControl => playerEntryControl.SetSelected(false, true));
        playerEntryControls.ForEach(playerEntryControl => playerEntryControl.SetSelected(true, true));
    }

    private void OnClientConnectionChanged(CompanionClientConnectionChangedEvent evt)
    {
        // Find existing or create new MicProfile for the newly connected device
        MicProfile connectedMicProfile = settings.MicProfiles.FirstOrDefault(it => it.ConnectedClientId == evt.CompanionClientHandler.ClientId);
        if (connectedMicProfile == null)
        {
            connectedMicProfile = new MicProfile(evt.CompanionClientHandler.ClientName, 0, evt.CompanionClientHandler.ClientId);
            settings.MicProfiles.Add(connectedMicProfile);
        }

        if (evt.IsConnected)
        {
            // Assign to player if needed
            UseMicProfileWhereNeeded(connectedMicProfile);
        }
        else if (!evt.IsConnected)
        {
            // Remove from players where already assigned
            RemoveMicProfileFromListEntries(connectedMicProfile);
        }

        // Refresh mic selection dialog.
        if (SongSelectPlayerEntryControl.MicSelectionDialogControl != null)
        {
            SongSelectPlayerEntryControl.MicSelectionDialogControl.MicProfiles = GetAvailableMicProfiles();
        }

        // Update MicPitchTrackers of all players
        playerEntryControls.ForEach(it => it.UpdateMicPitchTracker());
    }

    private void OnConnectedMicDevicesChanged(ConnectedMicDevicesChangedEvent evt)
    {
        // Assign newly connected mic devices to player if needed
        List<MicProfile> connectedMicProfiles = evt.ConnectedMicDevices
            .SelectMany(micDeviceName => SettingsUtils.GetMicProfiles(settings, micDeviceName))
            .ToList();
        foreach (MicProfile micProfile in connectedMicProfiles)
        {
            UseMicProfileWhereNeeded(micProfile);
        }

        // Remove newly disconnected players where already assigned
        List<MicProfile> disconnectedMicProfile = evt.DisconnectedMicDevices
            .SelectMany(micDeviceName => SettingsUtils.GetMicProfiles(settings, micDeviceName))
            .ToList();
        foreach (MicProfile micProfile in disconnectedMicProfile)
        {
            RemoveMicProfileFromListEntries(micProfile);
        }

        // Refresh mic selection dialog.
        if (SongSelectPlayerEntryControl.MicSelectionDialogControl != null)
        {
            SongSelectPlayerEntryControl.MicSelectionDialogControl.MicProfiles = GetAvailableMicProfiles();
        }

        // Update MicPitchTrackers of all players
        playerEntryControls.ForEach(it => it.UpdateMicPitchTracker());
    }

    private List<MicProfile> GetAvailableMicProfiles()
    {
        return SettingsUtils.GetAvailableMicProfiles(settings, themeManager, serverSideCompanionClientManager);
    }

    private void UpdateListEntries()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectPlayerListControl.UpdateListEntries");

        // Remove old entries
        playerList.Clear();
        playerEntryControls.Clear();

        // Create new entries
        List<PlayerProfile> enabledPlayerProfiles = songSelectSceneControl.GetEnabledPlayerProfiles();
        foreach (PlayerProfile playerProfile in enabledPlayerProfiles)
        {
            CreateListEntry(playerProfile);
        }
        UpdateVoiceSelection();
    }

    private void CreateListEntry(PlayerProfile playerProfile)
    {
        VisualElement playerEntryVisualElement = playerEntryUi.CloneTree().Children().FirstOrDefault();
        playerList.Add(playerEntryVisualElement);

        SongSelectPlayerEntryControl listEntryControl = injector
            .WithRootVisualElement(playerEntryVisualElement)
            .WithBindingForInstance(playerProfile)
            .WithBindingForInstance(PartyModeUtils.GetTeam(songSelectSceneControl.PartyModeSceneData, playerProfile))
            .CreateAndInject<SongSelectPlayerEntryControl>();
        listEntryControl.Init(playerProfile);

        listEntryControl.IsSelected.Value = playerProfile.IsSelected;
        listEntryControl.IsSelected.Subscribe(newValue => OnPlayerEntrySelectionStatusChanged(listEntryControl, newValue));
        listEntryControl.SetSelected(playerProfile.IsSelected, false);
        listEntryControl.OnMicProfileSelected = newMicProfile =>
        {
            // Remove this mic profile from other players
            playerEntryControls
                .Where(it => it != listEntryControl && it.MicProfile == newMicProfile)
                .ForEach(it => it.MicProfile = null);

            // Remember this mic
            settings.PlayerProfileNameToLastUsedMicProfile[playerProfile.Name] = new MicProfileReference(newMicProfile);
        };

        playerEntryControls.Add(listEntryControl);
    }

    private void UseMicProfileWhereNeeded(MicProfile micProfile)
    {
        if (micProfile == null
            || !micProfile.IsEnabled)
        {
            return;
        }

        List<SongSelectPlayerEntryControl> relevantPlayerEntryControls = playerEntryControls
            .Where(it => it.CanSelectMic)
            .ToList();

        SongSelectPlayerEntryControl listEntryControlWithMatchingMicProfile = relevantPlayerEntryControls.FirstOrDefault(it =>
            Equals(it.MicProfile, micProfile));
        if (listEntryControlWithMatchingMicProfile != null)
        {
            // Already in use. Cannot be assign to other players.
            return;
        }

        List<SongSelectPlayerEntryControl> listEntryControlsWithMissingMicProfile = relevantPlayerEntryControls
            .Where(it => it.PlayerProfile.IsSelected && it.MicProfile == null)
            .ToList();
        if (listEntryControlsWithMissingMicProfile.IsNullOrEmpty())
        {
            return;
        }

        // Prefer player with same name
        SongSelectPlayerEntryControl listEntryControlWithMissingMicProfileAndSameName = listEntryControlsWithMissingMicProfile.FirstOrDefault(it =>
            string.Equals(it.PlayerProfile.Name, micProfile.Name, StringComparison.InvariantCultureIgnoreCase));
        if (listEntryControlWithMissingMicProfileAndSameName != null)
        {
            listEntryControlWithMissingMicProfileAndSameName.MicProfile = micProfile;
            return;
        }

        // Prefer player that used this mic last time
        SongSelectPlayerEntryControl listEntryControlWithMissingMicProfileThatUsedThisMicProfileLastTime = listEntryControlsWithMissingMicProfile.FirstOrDefault(it =>
            settings.PlayerProfileNameToLastUsedMicProfile.TryGetValue(it.PlayerProfile.Name, out MicProfileReference lastUsedMicProfileReference)
                  && lastUsedMicProfileReference != null
                  && lastUsedMicProfileReference.Equals(micProfile));
        if (listEntryControlWithMissingMicProfileThatUsedThisMicProfileLastTime != null)
        {
            listEntryControlWithMissingMicProfileThatUsedThisMicProfileLastTime.MicProfile = micProfile;
            return;
        }

        // Assign to first player with missing mic profile
        SongSelectPlayerEntryControl listEntryControlWithMissingMicProfile = listEntryControlsWithMissingMicProfile.FirstOrDefault();
        if (listEntryControlWithMissingMicProfile != null)
        {
            listEntryControlWithMissingMicProfile.MicProfile = micProfile;
        }
    }

    private void RemoveMicProfileFromListEntries(MicProfile micProfile)
    {
        foreach (SongSelectPlayerEntryControl listEntry in playerEntryControls)
        {
            if (Equals(listEntry.MicProfile, micProfile))
            {
                listEntry.MicProfile = null;
            }
        }
    }

    private void OnPlayerEntrySelectionStatusChanged(SongSelectPlayerEntryControl listEntryControl, bool newValue)
    {
        listEntryControl.PlayerProfile.IsSelected = newValue;
        if (newValue == false)
        {
            listEntryControl.MicProfile = null;
        }
        else
        {
            MicProfile unusedMicProfile = GetUnusedMicProfileForPlayer(listEntryControl.PlayerProfile);
            if (unusedMicProfile != null)
            {
                listEntryControl.MicProfile = unusedMicProfile;
            }
        }
    }

    private MicProfile GetUnusedMicProfileForPlayer(PlayerProfile playerProfile)
    {
        List<MicProfile> unusedMicProfiles = FindUnusedMicProfiles();
        if (unusedMicProfiles.IsNullOrEmpty()
            || (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile
                && lobbyMemberPlayerProfile.IsRemote))
        {
            return null;
        }

        // Prefer MicProfile that was last used by this player
        if (settings.PlayerProfileNameToLastUsedMicProfile.TryGetValue(playerProfile.Name, out MicProfileReference lastUsedMicProfileReference)
            && lastUsedMicProfileReference != null)
        {
            MicProfile lastUsedMicProfile = unusedMicProfiles.FirstOrDefault(it => lastUsedMicProfileReference.Equals(it));
            if (lastUsedMicProfile != null)
            {
                return lastUsedMicProfile;
            }
        }

        // Prefer MicProfile with same name as player. This could be a mic from the Companion App.
        MicProfile micProfileWithMatchingName = unusedMicProfiles.FirstOrDefault(unusedMicProfile =>
            string.Equals(unusedMicProfile.Name, playerProfile.Name, StringComparison.InvariantCultureIgnoreCase));
        if (micProfileWithMatchingName != null)
        {
            return micProfileWithMatchingName;
        }

        // Ignore mic profiles that match other player names
        HashSet<string> playerNames = playerEntryControls
            .Select(otherPlayerEntryControl => otherPlayerEntryControl.PlayerProfile.Name)
            .ToHashSet();
        List<MicProfile> unusedMicProfilesNotMatchingAnyPlayerName = unusedMicProfiles
            .Where(unusedMicProfile => !playerNames.Contains(unusedMicProfile.Name))
            .ToList();

        return unusedMicProfilesNotMatchingAnyPlayerName.FirstOrDefault();
    }

    private List<MicProfile> FindUnusedMicProfiles()
    {
        List<MicProfile> usedMicProfiles = playerEntryControls.Where(it => it.MicProfile != null)
            .Select(it => it.MicProfile)
            .ToList();
        List<MicProfile> enabledAndConnectedMicProfiles = settings.MicProfiles
            .Where(it => it.IsEnabled && it.IsConnected(serverSideCompanionClientManager))
            .ToList();
        List<MicProfile> unusedMicProfiles = enabledAndConnectedMicProfiles
            .Where(it => !usedMicProfiles.Contains(it))
            .ToList();
        return unusedMicProfiles;
    }

    public List<PlayerProfile> GetSelectedPlayerProfiles()
    {
        List<PlayerProfile> result = playerEntryControls
            .Where(it => it.IsSelected.Value)
            .Select(it => it.PlayerProfile)
            .ToList();
        return result;
    }

    public Dictionary<PlayerProfile, MicProfile> GetSelectedPlayerProfileToMicProfileMap()
    {
        Dictionary<PlayerProfile, MicProfile> result = new();
        playerEntryControls.ForEach(entry =>
        {
            if (entry.IsSelected.Value
                && entry.CanSelectMic
                && entry.MicProfile != null)
            {
                result.Add(entry.PlayerProfile, entry.MicProfile);
            }
        });
        return result;
    }

    public Dictionary<PlayerProfile, EExtendedVoiceId> GetSelectedPlayerProfileToExtendedVoiceIdMap()
    {
        return playerEntryControls
            .Where(entry => entry.IsSelected.Value)
            .ToDictionary(entry => entry.PlayerProfile, entry => entry.VoiceId);
    }

    public void ToggleSelectedPlayers()
    {
        List<SongSelectPlayerEntryControl> deselectedEntries = new();
        // First deactivate the selected ones to make their mics available for others.
        playerEntryControls.ForEach(entry =>
        {
            if (entry.IsSelected.Value)
            {
                entry.IsSelected.Value = false;
            }
            else
            {
                deselectedEntries.Add(entry);
            }
        });
        // Second activate the ones that were deselected.
        // Because others have been deselected, they will be assigned free mics if any.
        foreach (SongSelectPlayerEntryControl entry in deselectedEntries)
        {
            entry.IsSelected.Value = true;
        }
    }

    private void LoadLastPlayerProfileToMicProfileMap()
    {
        if (settings.PlayerProfileNameToLastUsedMicProfile.IsNullOrEmpty())
        {
            return;
        }

        // Restore the previously assigned microphones
        List<MicProfile> availableMicProfiles = SettingsUtils.GetAvailableMicProfiles(settings, themeManager, serverSideCompanionClientManager);
        foreach (SongSelectPlayerEntryControl playerEntryControl in playerEntryControls)
        {
            if (!playerEntryControl.IsSelected.Value)
            {
                // Player is not selected for singing at the moment
                continue;
            }

            PlayerProfile playerProfile = playerEntryControl.PlayerProfile;
            if (!TryGetLastUsedMicProfile(availableMicProfiles, playerProfile.Name, out MicProfile lastUsedMicProfile))
            {
                // No mic was assigned to this player yet.
                continue;
            }

            if (!lastUsedMicProfile.IsConnected(serverSideCompanionClientManager)
                || !lastUsedMicProfile.IsEnabled)
            {
                // Mic cannot or should not be used at the moment.
                continue;
            }

            // Unassign mic from other players
            playerEntryControls
                .Except(new List<SongSelectPlayerEntryControl>() { playerEntryControl })
                .ForEach(otherEntry =>
                {
                    if (otherEntry.IsSelected.Value
                        && Equals(otherEntry.MicProfile, lastUsedMicProfile))
                    {
                        // Deselect lastUsedMicProfile from other player.
                        otherEntry.MicProfile = null;
                    }
                });

            // Assign mic to this player
            playerEntryControl.MicProfile = lastUsedMicProfile;
        }
    }

    private bool TryGetLastUsedMicProfile(List<MicProfile> availableMicProfiles, string playerProfileName, out MicProfile micProfile)
    {
        if (settings.PlayerProfileNameToLastUsedMicProfile.TryGetValue(playerProfileName, out MicProfileReference micProfileReference)
            && micProfileReference != null)
        {
            micProfile = availableMicProfiles.FirstOrDefault(availableMicProfile =>
                micProfileReference.Equals(availableMicProfile));
            return micProfile != null;
        }

        micProfile = null;
        return false;
    }

    private void UpdatePlayerProfileNameToLastUsedMicProfile()
    {
        foreach (SongSelectPlayerEntryControl playerEntryControl in playerEntryControls)
        {
            if (playerEntryControl.MicProfile != null
                && playerEntryControl.CanSelectMic)
            {
                string playerProfileName = playerEntryControl.PlayerProfile.Name;
                MicProfileReference micProfileReference = new(playerEntryControl.MicProfile);
                settings.PlayerProfileNameToLastUsedMicProfile[playerProfileName] = micProfileReference;
            }
        }
    }

    private void OnDestroy()
    {
        // Remember the currently assigned microphones
        UpdatePlayerProfileNameToLastUsedMicProfile();
        playerEntryControls.ForEach(control => control.Dispose());
    }

    private void HideVoiceSelection()
    {
        playerEntryControls.ForEach(entry =>
        {
            entry.HideVoiceSelection();
        });
    }

    private void ShowVoiceSelection(SongMeta songMeta)
    {
        int voiceIndex = 0;
        playerEntryControls.ForEach(entry =>
        {
            entry.ShowVoiceSelection(songMeta, voiceIndex);
            voiceIndex = (voiceIndex + 1) % songMeta.VoiceCount;
        });
    }

    public void UpdateVoiceSelection()
    {
        SongMeta selectedSong = songSelectSceneControl.SelectedSong;
        bool hasMultipleVoices = selectedSong != null
            && selectedSong.Voices.Count > 1;
        if (hasMultipleVoices)
        {
            ShowVoiceSelection(selectedSong);
        }
        else
        {
            HideVoiceSelection();
        }
    }
}
