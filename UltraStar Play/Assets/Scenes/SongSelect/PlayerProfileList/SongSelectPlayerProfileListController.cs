using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

public class SongSelectPlayerProfileListController : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        lastPlayerProfileToMicProfileMap = null;
    }

    // Static reference to be persisted across scenes.
    // Used to restore the player-microphone assignment.
    private static PlayerProfileToMicProfileMap lastPlayerProfileToMicProfileMap;

    public SongSelectPlayerProfileListEntry listEntryPrefab;
    public GameObject scrollViewContent;

    private readonly List<SongSelectPlayerProfileListEntry> listEntries = new List<SongSelectPlayerProfileListEntry>();

    void Start()
    {
        UpdateListEntries();
        LoadLastPlayerProfileToMicProfileMap();
    }

    private void UpdateListEntries()
    {
        // Remove old entries
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }
        listEntries.Clear();

        // Create new entries
        List<PlayerProfile> playerProfiles = SettingsManager.Instance.Settings.PlayerProfiles;
        List<PlayerProfile> enabledPlayerProfiles = playerProfiles.Where(it => it.IsEnabled).ToList();
        foreach (PlayerProfile playerProfile in enabledPlayerProfiles)
        {
            CreateListEntry(playerProfile);
        }
    }

    private void CreateListEntry(PlayerProfile playerProfile)
    {
        SongSelectPlayerProfileListEntry listEntry = Instantiate(listEntryPrefab, scrollViewContent.transform);
        listEntry.Init(playerProfile);

        listEntry.SetSelected(playerProfile.IsSelected);
        listEntry.isSelectedToggle.OnValueChangedAsObservable().Subscribe(newValue => OnSelectionStatusChanged(listEntry, newValue));

        listEntries.Add(listEntry);
    }

    private void OnSelectionStatusChanged(SongSelectPlayerProfileListEntry listEntry, bool newValue)
    {
        listEntry.PlayerProfile.IsSelected = newValue;
        if (newValue == false)
        {
            listEntry.MicProfile = null;
        }
        else
        {
            List<MicProfile> unusedMicProfiles = FindUnusedMicProfiles();
            if (!unusedMicProfiles.IsNullOrEmpty())
            {
                listEntry.MicProfile = unusedMicProfiles[0];
            }
        }
    }

    private List<MicProfile> FindUnusedMicProfiles()
    {
        List<MicProfile> usedMicProfiles = listEntries.Where(it => it.MicProfile != null).Select(it => it.MicProfile).ToList();
        List<MicProfile> enabledAndConnectedMicProfiles = SettingsManager.Instance.Settings.MicProfiles.Where(it => it.IsEnabled && it.IsConnected).ToList();
        List<MicProfile> unusedMicProfiles = enabledAndConnectedMicProfiles.Where(it => !usedMicProfiles.Contains(it)).ToList();
        return unusedMicProfiles;
    }

    public List<PlayerProfile> GetSelectedPlayerProfiles()
    {
        SongSelectPlayerProfileListEntry[] listEntriesInScrollView = scrollViewContent.GetComponentsInChildren<SongSelectPlayerProfileListEntry>();
        List<PlayerProfile> result = listEntriesInScrollView.Where(it => it.IsSelected).Select(it => it.PlayerProfile).ToList();
        return result;
    }

    public PlayerProfileToMicProfileMap GetSelectedPlayerProfileToMicProfileMap()
    {
        PlayerProfileToMicProfileMap result = new PlayerProfileToMicProfileMap();
        SongSelectPlayerProfileListEntry[] listEntries = scrollViewContent.GetComponentsInChildren<SongSelectPlayerProfileListEntry>();
        foreach (SongSelectPlayerProfileListEntry entry in listEntries)
        {
            if (entry.IsSelected && entry.MicProfile != null)
            {
                result.Add(entry.PlayerProfile, entry.MicProfile);
            }
        }
        return result;
    }

    public void ToggleSelectedPlayers()
    {
        SongSelectPlayerProfileListEntry[] listEntriesInScrollView = scrollViewContent.GetComponentsInChildren<SongSelectPlayerProfileListEntry>();
        List<SongSelectPlayerProfileListEntry> deselectedEntries = new List<SongSelectPlayerProfileListEntry>();
        // First deactivate the selected ones to make their mics available for others.
        foreach (SongSelectPlayerProfileListEntry entry in listEntriesInScrollView)
        {
            if (entry.IsSelected)
            {
                entry.SetSelected(false);
            }
            else
            {
                deselectedEntries.Add(entry);
            }
        }
        // Second activate the ones that were deselected.
        // Because others have been deselected, they will be assigned free mics if any.
        foreach (SongSelectPlayerProfileListEntry entry in deselectedEntries)
        {
            entry.SetSelected(true);
        }
    }

    private void LoadLastPlayerProfileToMicProfileMap()
    {
        if (lastPlayerProfileToMicProfileMap.IsNullOrEmpty())
        {
            return;
        }

        // Restore the previously assigned microphones
        SongSelectPlayerProfileListEntry[] listEntriesInScrollView = scrollViewContent.GetComponentsInChildren<SongSelectPlayerProfileListEntry>();
        foreach (KeyValuePair<PlayerProfile, MicProfile> playerProfileAndMicProfileEntry in lastPlayerProfileToMicProfileMap)
        {
            PlayerProfile playerProfile = playerProfileAndMicProfileEntry.Key;
            MicProfile lastUsedMicProfile = playerProfileAndMicProfileEntry.Value;

            if (!lastUsedMicProfile.IsConnected
                || !lastUsedMicProfile.IsEnabled)
            {
                // Do not use this mic.
                continue;
            }

            foreach (SongSelectPlayerProfileListEntry listEntry in listEntriesInScrollView)
            {
                if (listEntry.PlayerProfile == playerProfile)
                {
                    // Select the mic for this player
                    listEntry.MicProfile = lastUsedMicProfile;
                }
                else if (listEntry.IsSelected
                         && listEntry.MicProfile == lastUsedMicProfile)
                {
                    // Deselect lastUsedMicProfile from other player.
                    listEntry.MicProfile = null;
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Remember the currently assigned microphones
        lastPlayerProfileToMicProfileMap = GetSelectedPlayerProfileToMicProfileMap();
    }
}
