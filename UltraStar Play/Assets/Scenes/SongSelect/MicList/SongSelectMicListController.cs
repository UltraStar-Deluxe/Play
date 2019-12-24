using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SongSelectMicListController : MonoBehaviour, IOnHotSwapFinishedListener
{
    public SongSelectMicListEntry listEntryPrefab;
    public GameObject scrollViewContent;
    public GameObject emptyListLabel;

    void Start()
    {
        UpdateListEntries();
    }

    public void OnHotSwapFinished()
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

        // Create new entries
        List<MicProfile> micProfiles = SettingsManager.Instance.Settings.MicProfiles;
        List<MicProfile> enabledAndConnectedMicProfiles = micProfiles.Where(it => it.IsEnabled && it.IsConnected).ToList();
        if (enabledAndConnectedMicProfiles.IsNullOrEmpty())
        {
            emptyListLabel.SetActive(true);
        }
        else
        {
            emptyListLabel.SetActive(false);
            foreach (MicProfile micProfile in enabledAndConnectedMicProfiles)
            {
                CreateListEntry(micProfile);
            }
        }
    }

    private void CreateListEntry(MicProfile micProfile)
    {
        SongSelectMicListEntry listEntry = Instantiate(listEntryPrefab, scrollViewContent.transform);
        listEntry.Init(micProfile);
    }
}
