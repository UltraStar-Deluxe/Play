using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMicListController : MonoBehaviour, IOnHotSwapFinishedListener, INeedInjection
{
    [InjectedInInspector]
    public SongSelectMicListEntry listEntryPrefab;
    
    [InjectedInInspector]
    public GameObject scrollViewContent;
    
    [InjectedInInspector]
    public GameObject emptyListLabel;

    [Inject]
    private Injector injector;
    
    [Inject]
    private Settings settings;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private readonly List<SongSelectMicListEntry> listEntries = new List<SongSelectMicListEntry>();

    void Start()
    {
        UpdateListEntries();
        
        // Remove/add MicProfile when Client (dis)connects.
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(HandleClientConnectedEvent)
            .AddTo(gameObject);
    }

    private void HandleClientConnectedEvent(ClientConnectionEvent connectionEvent)
    {
        // Find existing or create new MicProfile for the newly connected device
        MicProfile micProfile = settings.MicProfiles.FirstOrDefault(it => it.ConnectedClientId == connectionEvent.ConnectedClientHandler.ClientId);
        if (micProfile == null)
        {
            micProfile = new MicProfile(connectionEvent.ConnectedClientHandler.ClientName, connectionEvent.ConnectedClientHandler.ClientId);
            settings.MicProfiles.Add(micProfile);
        }
        
        SongSelectMicListEntry matchingListEntry = listEntries.FirstOrDefault(listEntry => 
               listEntry.MicProfile != null
            && listEntry.MicProfile.ConnectedClientId == connectionEvent.ConnectedClientHandler.ClientId
            && listEntry.MicProfile.IsEnabled);
        if (connectionEvent.IsConnected && matchingListEntry == null && micProfile.IsEnabled)
        {
            // Add to UI
            CreateListEntry(micProfile);
        }
        else if (!connectionEvent.IsConnected && matchingListEntry != null)
        {
            // Remove from UI
            RemoveListEntry(matchingListEntry);
        }
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
        List<MicProfile> micProfiles = settings.MicProfiles;
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
        listEntry.MicProfile = micProfile;

        listEntries.Add(listEntry);
        
        emptyListLabel.SetActive(false);
    }

    private void RemoveListEntry(SongSelectMicListEntry listEntry)
    {
        Destroy(listEntry.gameObject);
        listEntries.Remove(listEntry);
        if (listEntries.IsNullOrEmpty())
        {
            emptyListLabel.SetActive(true);
        }
    }
}
