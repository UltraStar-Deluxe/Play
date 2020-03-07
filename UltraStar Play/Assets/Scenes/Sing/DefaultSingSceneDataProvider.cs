using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This class handles the creation of a sensible default for the SingSceneData
// when starting the SingScene from within the Unity editor.
public class DefaultSingSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public string defaultSongName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public SceneData GetDefaultSceneData()
    {
        SingSceneData defaultSceneData = new SingSceneData();
        defaultSceneData.SelectedSongMeta = GetDefaultSongMeta();

        PlayerProfile playerProfile = GetDefaultPlayerProfile();
        defaultSceneData.SelectedPlayerProfiles.Add(playerProfile);
        defaultSceneData.PlayerProfileToMicProfileMap[playerProfile] = GetDefaultMicProfile();

        return defaultSceneData;
    }

    private PlayerProfile GetDefaultPlayerProfile()
    {
        List<PlayerProfile> allPlayerProfiles = SettingsManager.Instance.Settings.PlayerProfiles;
        if (allPlayerProfiles.IsNullOrEmpty())
        {
            throw new UnityException("No player profiles found.");
        }
        PlayerProfile result = allPlayerProfiles[0];
        return result;
    }

    private MicProfile GetDefaultMicProfile()
    {
        return SettingsManager.Instance.Settings.MicProfiles.Where(it => it.IsEnabled && it.IsConnected).FirstOrDefault();
    }

    private SongMeta GetDefaultSongMeta()
    {
        // The default song meta is for debugging in the Unity editor.
        // A specific song is searched, so first wait for the scan to complete.
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        SongMeta defaultSongMeta = SongMetaManager.Instance.FindSongMeta(it => it.Title.Trim() == defaultSongName.Trim());
        if (defaultSongMeta == null)
        {
            Debug.LogWarning($"No song with title '{defaultSongName.Trim()}' was found. Using the first found song instead.");
            return SongMetaManager.Instance.GetFirstSongMeta();
        }
        return defaultSongMeta;
    }
}
