using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectPlayerProfileListController : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        lastPlayerProfileToMicProfileMap = null;
    }

    // Static reference to be persisted across scenes.
    // Used to restore the player-microphone assignment.
    private static PlayerProfileToMicProfileMap lastPlayerProfileToMicProfileMap;

    [InjectedInInspector]
    public SongSelectPlayerProfileListEntry listEntryPrefab;
    
    [InjectedInInspector]
    public GameObject scrollViewContent;

    private readonly List<SongSelectPlayerProfileListEntry> listEntries = new List<SongSelectPlayerProfileListEntry>();
    public List<SongSelectPlayerProfileListEntry> PlayerProfileControls => listEntries;

    public SongSelectPlayerProfileListEntry FocusedPlayerProfileControl => PlayerProfileControls
        .FirstOrDefault(it => eventSystem.currentSelectedGameObject == it.isSelectedToggle.gameObject);

    public int FocusedPlayerProfileControlIndex => PlayerProfileControls.IndexOf(FocusedPlayerProfileControl);
    
    [Inject]
    private EventSystem eventSystem;
    
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;
    
    [Inject]
    private Settings settings;

    private readonly List<IDisposable> disposables = new List<IDisposable>();
    
    void Start()
    {
        UpdateListEntries();
        LoadLastPlayerProfileToMicProfileMap();
        
        // Remove/add MicProfile when Client (dis)connects.
        disposables.Add(serverSideConnectRequestManager.ClientConnectedEventStream.Subscribe(HandleClientConnectedEvent));
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
        
        if (connectionEvent.IsConnected)
        {
            // Assign to player if needed
            UseMicProfileWhereNeeded(micProfile);
        }
        else if (!connectionEvent.IsConnected)
        {
            // Remove from players where already assigned
            RemoveMicProfileFromListEntries(micProfile);
        }
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

    private void UseMicProfileWhereNeeded(MicProfile micProfile)
    {
        SongSelectPlayerProfileListEntry listEntryWithMatchingMicProfile = listEntries.FirstOrDefault(it => 
               it.MicProfile != null
            && it.MicProfile.ConnectedClientId == micProfile.ConnectedClientId);
        if (listEntryWithMatchingMicProfile != null)
        {
            // Already in use. Cannot be assign to other players.
            return;
        }
        
        SongSelectPlayerProfileListEntry listEntryWithMissingMicProfile = listEntries.FirstOrDefault(it => it.PlayerProfile.IsSelected && it.MicProfile == null);
        if (listEntryWithMissingMicProfile != null)
        {
            listEntryWithMissingMicProfile.MicProfile = micProfile;
        }
    }
    
    private void RemoveMicProfileFromListEntries(MicProfile micProfile)
    {
        foreach (SongSelectPlayerProfileListEntry listEntry in listEntries)
        {
            if (listEntry.MicProfile != null
                && listEntry.MicProfile.ConnectedClientId == micProfile.ConnectedClientId)
            {
                listEntry.MicProfile = null;
            }
        }
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
        disposables.ForEach(it => it.Dispose());
    }

    public bool TrySelectNextControl()
    {
        if ((eventSystem.currentSelectedGameObject == null
            || eventSystem.currentSelectedGameObject.GetComponentInParent<SongSelectPlayerProfileListEntry>() == null)
            && PlayerProfileControls.Count > 0)
        {
            PlayerProfileControls.First().isSelectedToggle.Select();
            return true;
        }
            
        SongSelectPlayerProfileListEntry nextEntry = PlayerProfileControls.GetElementAfter(FocusedPlayerProfileControl, false);
        if (nextEntry != null)
        {
            nextEntry.isSelectedToggle.Select();
            return true;
        }

        return false;
    }
    
    public bool TrySelectPreviousControl()
    {
        if ((eventSystem.currentSelectedGameObject == null
            || eventSystem.currentSelectedGameObject.GetComponentInParent<SongSelectPlayerProfileListEntry>() == null)
            && PlayerProfileControls.Count > 0)
        {
            PlayerProfileControls.Last().isSelectedToggle.Select();
            return true;
        }
        
        SongSelectPlayerProfileListEntry nextEntry = PlayerProfileControls.GetElementBefore(FocusedPlayerProfileControl, false);
        if (nextEntry != null)
        {
            nextEntry.isSelectedToggle.Select();
            return true;
        }
        
        return false;
    }
}
