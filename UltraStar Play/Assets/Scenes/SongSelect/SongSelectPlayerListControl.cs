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
    static void StaticInit()
    {
        lastPlayerProfileToMicProfileMap = null;
    }

    // Static reference to be persisted across scenes.
    // Used to restore the player-microphone assignment.
    private static Dictionary<PlayerProfile, MicProfile> lastPlayerProfileToMicProfileMap;

    [InjectedInInspector]
    public VisualTreeAsset playerEntryUi;

    [Inject(UxmlName = R.UxmlNames.playerScrollView)]
    public VisualElement playerScrollView;

    private readonly List<SongSelectPlayerEntryControl> playerEntryControls = new();
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
        List<PlayerProfile> playerProfiles = settings.PlayerProfiles;
        List<PlayerProfile> enabledPlayerProfiles = playerProfiles.Where(it => it.IsEnabled).ToList();
        foreach (PlayerProfile playerProfile in enabledPlayerProfiles)
        {
            CreateListEntry(playerProfile);
        }
        UpdateVoiceSelection();
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(playerScrollView);
    }

    private void CreateListEntry(PlayerProfile playerProfile)
    {
        VisualElement playerEntryVisualElement = playerEntryUi.CloneTree().Children().FirstOrDefault();
        playerScrollView.Add(playerEntryVisualElement);

        SongSelectPlayerEntryControl listEntryControl = injector
            .WithRootVisualElement(playerEntryVisualElement)
            .CreateAndInject<SongSelectPlayerEntryControl>();
        listEntryControl.Init(playerProfile);

        listEntryControl.IsSelected.Value = playerProfile.IsSelected;
        listEntryControl.IsSelected.Subscribe(newValue => OnSelectionStatusChanged(listEntryControl, newValue));

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
        List<MicProfile> enabledAndConnectedMicProfiles = settings.MicProfiles
            .Where(it => it.IsEnabled && it.IsConnected(serverSideConnectRequestManager))
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
            if (entry.IsSelected.Value && entry.MicProfile != null)
            {
                result.Add(entry.PlayerProfile, entry.MicProfile);
            }
        });
        return result;
    }

    public Dictionary<PlayerProfile,string> GetSelectedPlayerProfileToVoiceNameMap()
    {
        Dictionary<PlayerProfile,string> selectedPlayerProfileToVoiceNameMap = new();
        playerEntryControls.ForEach(entry =>
        {
            if (entry.IsSelected.Value)
            {
                string voiceName = !entry.VoiceName.IsNullOrEmpty()
                    ? entry.VoiceName
                    : Voice.soloVoiceName;
                selectedPlayerProfileToVoiceNameMap.Add(entry.PlayerProfile, voiceName);
            }
        });
        return selectedPlayerProfileToVoiceNameMap;
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
        if (lastPlayerProfileToMicProfileMap.IsNullOrEmpty())
        {
            return;
        }

        // Restore the previously assigned microphones
        foreach (KeyValuePair<PlayerProfile, MicProfile> playerProfileAndMicProfileEntry in lastPlayerProfileToMicProfileMap)
        {
            PlayerProfile playerProfile = playerProfileAndMicProfileEntry.Key;
            MicProfile lastUsedMicProfile = playerProfileAndMicProfileEntry.Value;

            if (!lastUsedMicProfile.IsConnected(serverSideConnectRequestManager)
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
                else if (entry.IsSelected.Value
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
            voiceIndex = (voiceIndex + 1) % songMeta.VoiceNames.Count;
        });
    }
    
    public void UpdateVoiceSelection()
    {
        SongMeta selectedSong = songRouletteControl.Selection.Value.SongMeta;
        bool hasMultipleVoices = selectedSong != null
            && selectedSong.VoiceNames.Count > 1;
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
