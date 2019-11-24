using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

public class SongSelectPlayerProfileListController : MonoBehaviour
{
    public SongSelectPlayerProfileListEntry listEntryPrefab;
    public GameObject scrollViewContent;

    private readonly List<SongSelectPlayerProfileListEntry> listEntries = new List<SongSelectPlayerProfileListEntry>();

    void Start()
    {
        UpdateListEntries();
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
        SongSelectPlayerProfileListEntry listEntry = Instantiate(listEntryPrefab);
        listEntry.transform.SetParent(scrollViewContent.transform);
        listEntry.Init(playerProfile);

        listEntry.isSelectedToggle.OnValueChangedAsObservable().Subscribe(newValue => OnSelectionStatusChanged(listEntry, newValue));

        listEntries.Add(listEntry);
    }

    private void OnSelectionStatusChanged(SongSelectPlayerProfileListEntry listEntry, bool newValue)
    {
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
}
