using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectPlayerListControl : MonoBehaviour, INeedInjection
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
    public VisualTreeAsset playerEntryUi;

    [Inject(UxmlName = R.UxmlNames.playerScrollView)]
    public VisualElement playerScrollView;

    private readonly List<SongSelectPlayerEntryControl> playerEntryControls = new List<SongSelectPlayerEntryControl>();
    public List<SongSelectPlayerEntryControl> PlayerEntryControlControls => playerEntryControls;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;
    
    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private SongRouletteControl songRouletteControl;

    void Start()
    {
        UpdateListEntries();
        LoadLastPlayerProfileToMicProfileMap();
        
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
        playerScrollView.Clear();
        playerEntryControls.Clear();

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
        VisualElement playerEntryVisualElement = playerEntryUi.CloneTree().Children().FirstOrDefault();
        playerScrollView.Add(playerEntryVisualElement);

        SongSelectPlayerEntryControl listEntryControl = injector
            .WithRootVisualElement(playerEntryVisualElement)
            .CreateAndInject<SongSelectPlayerEntryControl>();
        listEntryControl.Init(playerProfile);

        listEntryControl.EnabledToggle.RegisterValueChangedCallback(evt => OnSelectionStatusChanged(listEntryControl, evt.newValue));
        listEntryControl.SetSelected(playerProfile.IsSelected);

        playerEntryControls.Add(listEntryControl);
    }

    private void UseMicProfileWhereNeeded(MicProfile micProfile)
    {
        if (micProfile == null
            || !micProfile.IsEnabled)
        {
            return;
        }
        
        SongSelectPlayerEntryControl listEntryControlWithMatchingMicProfile = playerEntryControls.FirstOrDefault(it =>
               it.MicProfile != null
            && it.MicProfile.ConnectedClientId == micProfile.ConnectedClientId);
        if (listEntryControlWithMatchingMicProfile != null)
        {
            // Already in use. Cannot be assign to other players.
            return;
        }
        
        SongSelectPlayerEntryControl listEntryControlWithMissingMicProfile = playerEntryControls.FirstOrDefault(it => it.PlayerProfile.IsSelected && it.MicProfile == null);
        if (listEntryControlWithMissingMicProfile != null)
        {
            listEntryControlWithMissingMicProfile.MicProfile = micProfile;
        }
    }
    
    private void RemoveMicProfileFromListEntries(MicProfile micProfile)
    {
        foreach (SongSelectPlayerEntryControl listEntry in playerEntryControls)
        {
            if (listEntry.MicProfile != null
                && listEntry.MicProfile.ConnectedClientId == micProfile.ConnectedClientId)
            {
                listEntry.MicProfile = null;
            }
        }
    }
    
    private void OnSelectionStatusChanged(SongSelectPlayerEntryControl listEntryControl, bool newValue)
    {
        listEntryControl.PlayerProfile.IsSelected = newValue;
        if (newValue == false)
        {
            listEntryControl.MicProfile = null;
        }
        else
        {
            List<MicProfile> unusedMicProfiles = FindUnusedMicProfiles();
            if (!unusedMicProfiles.IsNullOrEmpty())
            {
                listEntryControl.MicProfile = unusedMicProfiles[0];
            }
        }
    }

    private List<MicProfile> FindUnusedMicProfiles()
    {
        List<MicProfile> usedMicProfiles = playerEntryControls.Where(it => it.MicProfile != null)
            .Select(it => it.MicProfile)
            .ToList();
        List<MicProfile> enabledAndConnectedMicProfiles = SettingsManager.Instance.Settings.MicProfiles
            .Where(it => it.IsEnabled && it.IsConnected)
            .ToList();
        List<MicProfile> unusedMicProfiles = enabledAndConnectedMicProfiles
            .Where(it => !usedMicProfiles.Contains(it))
            .ToList();
        return unusedMicProfiles;
    }

    public List<PlayerProfile> GetSelectedPlayerProfiles()
    {
        List<PlayerProfile> result = playerEntryControls
            .Where(it => it.IsSelected)
            .Select(it => it.PlayerProfile)
            .ToList();
        return result;
    }

    public PlayerProfileToMicProfileMap GetSelectedPlayerProfileToMicProfileMap()
    {
        PlayerProfileToMicProfileMap result = new PlayerProfileToMicProfileMap();
        playerEntryControls.ForEach(entry =>
        {
            if (entry.IsSelected && entry.MicProfile != null)
            {
                result.Add(entry.PlayerProfile, entry.MicProfile);
            }
        });
        return result;
    }

    public Dictionary<PlayerProfile,string> GetSelectedPlayerProfileToVoiceNameMap()
    {
        Dictionary<PlayerProfile,string> selectedPlayerProfileToVoiceNameMap = new Dictionary<PlayerProfile,string>();
        playerEntryControls.ForEach(entry =>
        {
            if (entry.IsSelected)
            {
                string voiceName = entry.Voice != null
                    ? entry.Voice.Name
                    : Voice.soloVoiceName;
                selectedPlayerProfileToVoiceNameMap.Add(entry.PlayerProfile, voiceName);
            }
        });
        return selectedPlayerProfileToVoiceNameMap;
    }

    public void ToggleSelectedPlayers()
    {
        List<SongSelectPlayerEntryControl> deselectedEntries = new List<SongSelectPlayerEntryControl>();
        // First deactivate the selected ones to make their mics available for others.
        playerEntryControls.ForEach(entry =>
        {
            if (entry.IsSelected)
            {
                entry.SetSelected(false);
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

            playerEntryControls.ForEach(entry =>
            {
                if (entry.PlayerProfile == playerProfile)
                {
                    // Select the mic for this player
                    entry.MicProfile = lastUsedMicProfile;
                }
                else if (entry.IsSelected
                         && entry.MicProfile == lastUsedMicProfile)
                {
                    // Deselect lastUsedMicProfile from other player.
                    entry.MicProfile = null;
                }
            });
        }
    }

    private void OnDestroy()
    {
        // Remember the currently assigned microphones
        lastPlayerProfileToMicProfileMap = GetSelectedPlayerProfileToMicProfileMap();
    }

    public void HideVoiceSelection()
    {
        playerEntryControls.ForEach(entry =>
        {
            entry.HideVoiceSelection();
        });
    }

    public void ShowVoiceSelection(SongMeta selectedSong)
    {
        int voiceIndex = 0;
        playerEntryControls.ForEach(entry =>
        {
            entry.ShowVoiceSelection(selectedSong, voiceIndex);
            voiceIndex = (voiceIndex + 1) % selectedSong.VoiceNames.Count;
        });
    }
}
