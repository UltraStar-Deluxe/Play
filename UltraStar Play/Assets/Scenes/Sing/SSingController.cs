using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SSingController : MonoBehaviour
{
    public string defaultSongName;
    public string defaultPlayerProfileName;

    private SongMeta songMeta;
    private PlayerProfile playerProfile;

    void Start()
    {
        songMeta = SceneDataBus.GetData(ESceneData.Song, GetDefaultSongMeta );
        playerProfile = SceneDataBus.GetData(ESceneData.PlayerProfile, GetDefaultPlayerProfile );

        Debug.Log($"{playerProfile.Name} starts singing of {songMeta.Title}.");
    }

    private PlayerProfile GetDefaultPlayerProfile()
    {
        var defaultPlayerProfiles = PlayerProfilesManager.PlayerProfiles.Where(it => it.Name == defaultPlayerProfileName);
        if(defaultPlayerProfiles.Count() == 0) {
            throw new Exception("The default player profile was not found.");
        }
        return defaultPlayerProfiles.First();
    }

    private SongMeta GetDefaultSongMeta()
    {
        SongMetaManager.ScanFiles();
        var defaultSongMetas = SongMetaManager.GetSongMetas().Where(it => it.Title == defaultSongName);
        if(defaultSongMetas.Count() == 0) {
            throw new Exception("The default song was not found.");
        }
        return defaultSongMetas.First();
    }
}
