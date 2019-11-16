using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SingSceneData : SceneData
{
    public SongMeta SelectedSongMeta { get; set; }
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; } = new List<PlayerProfile>();
    public Dictionary<PlayerProfile, MicProfile> PlayerProfileToMicProfileMap = new Dictionary<PlayerProfile, MicProfile>();
    public double PositionInSongMillis { get; set; }
    public bool IsRestart { get; set; }
}