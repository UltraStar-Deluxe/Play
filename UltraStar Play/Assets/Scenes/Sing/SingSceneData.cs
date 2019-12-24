using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SingSceneData : SceneData
{
    public SongMeta SelectedSongMeta { get; set; }
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; } = new List<PlayerProfile>();
    public PlayerProfileToMicProfileMap PlayerProfileToMicProfileMap { get; set; } = new PlayerProfileToMicProfileMap();
    public double PositionInSongInMillis { get; set; }
    public bool IsRestart { get; set; }
}