using System.Collections.Generic;
using UnityEngine;

public class DefaultSongEditorSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public string defaultSongName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public SceneData GetDefaultSceneData()
    {
        SongEditorSceneData defaultSceneData = new SongEditorSceneData();
        defaultSceneData.PositionInSongInMillis = 0;
        defaultSceneData.SelectedSongMeta = GetDefaultSongMeta();

        // Set up PreviousSceneData to directly start the SingScene.
        defaultSceneData.PreviousScene = EScene.SingScene;

        SingSceneData singSceneData = new SingSceneData();
        singSceneData.SelectedSongMeta = defaultSceneData.SelectedSongMeta;
        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
        playerProfiles.Add(playerProfile);
        singSceneData.SelectedPlayerProfiles = playerProfiles;

        defaultSceneData.PlayerProfileToMicProfileMap = singSceneData.PlayerProfileToMicProfileMap;
        defaultSceneData.SelectedPlayerProfiles = singSceneData.SelectedPlayerProfiles;
        defaultSceneData.PreviousSceneData = singSceneData;

        return defaultSceneData;
    }

    private SongMeta GetDefaultSongMeta()
    {
        // The default song meta is for debugging in the Unity editor.
        // A specific song is searched, so first wait for the scan to complete.
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        SongMeta defaultSongMeta = SongMetaManager.Instance.FindSongMeta(it => it.Title == GetDefaultSongName());
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
