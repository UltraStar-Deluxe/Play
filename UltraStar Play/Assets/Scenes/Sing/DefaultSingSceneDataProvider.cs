using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

// This class handles the creation of a sensible default for the SingSceneData
// when starting the SingScene from within the Unity editor.
public class DefaultSingSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider, INeedInjection
{
    public bool isMedley;

    [Range(1, 16)]
    public int playerCount = 1;
    
    public string defaultSongName;

    [TextArea(10, 20)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public SceneData GetDefaultSceneData()
    {
        SingSceneData defaultSceneData = new();
        defaultSceneData.SongMetas = new List<SongMeta> { GetDefaultSongMeta() };
        defaultSceneData.MedleySongIndex = isMedley ? 0 : -1;

        for (int i = 0; i < playerCount; i++)
        {
            PlayerProfile playerProfile = GetPlayerProfile(i);
            if (playerProfile == null)
            {
                break;
            }
            
            defaultSceneData.SingScenePlayerData.SelectedPlayerProfiles.Add(playerProfile);
            defaultSceneData.SingScenePlayerData.PlayerProfileToMicProfileMap[playerProfile] = GetMicProfile(i);
        }

        return defaultSceneData;
    }

    private PlayerProfile GetPlayerProfile(int index)
    {
        List<PlayerProfile> allPlayerProfiles = SettingsManager.Instance.Settings.PlayerProfiles;
        if (index >= allPlayerProfiles.Count)
        {
            return null;
        }
        
        return allPlayerProfiles[index];
    }

    private MicProfile GetMicProfile(int index)
    {
        List<MicProfile> micProfiles = SettingsManager.Instance.Settings.MicProfiles
            .Where(it => it.IsEnabled && it.IsConnected(ServerSideCompanionClientManager.Instance))
            .ToList();
        if (index >= micProfiles.Count)
        {
            return null;
        }

        return micProfiles[index];
    }

    private SongMeta GetDefaultSongMeta()
    {
        // The default song meta is for debugging in the Unity editor.
        // A specific song is searched, so first wait for the scan to complete.
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        SongMeta defaultSongMeta = SongMetaManager.Instance.GetSongMetas()
            .FirstOrDefault(it => it.Title.Trim() == GetDefaultSongName());
        if (defaultSongMeta == null)
        {
            Debug.LogWarning($"No song with title '{GetDefaultSongName()}' was found. Using the first found song instead.");
            return SongMetaManager.Instance.GetFirstSongMeta();
        }
        return defaultSongMeta;
    }

    private string GetDefaultSongName()
    {
        return defaultSongName.Trim();
    }
}
