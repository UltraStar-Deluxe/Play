using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SingSceneData : SceneData
{
    public SongMeta SelectedSongMeta { get; set; }
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; }
    public double PositionInSongMillis { get; set; }
    public bool IsRestart { get; set; }

    public void AddPlayerProfile(PlayerProfile playerProfile)
    {
        if (SelectedPlayerProfiles == null)
        {
            SelectedPlayerProfiles = new List<PlayerProfile>();
        }
        SelectedPlayerProfiles.Add(playerProfile);
    }
}