using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMicListController : MonoBehaviour, IOnHotSwapFinishedListener, INeedInjection
{
    public SongSelectMicListEntry listEntryPrefab;
    public GameObject scrollViewContent;
    public GameObject emptyListLabel;

    [Inject]
    private Injector injector;

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
        injector.InjectAllComponentsInChildren(listEntry);
        listEntry.Init(micProfile);
    }
}
