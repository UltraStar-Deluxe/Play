using System;
using System.Collections.Generic;

[Serializable]
public class SingSceneData : SceneData
{
    public SongMeta SelectedSongMeta { get; set; }
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; } = new List<PlayerProfile>();
    public PlayerProfileToMicProfileMap PlayerProfileToMicProfileMap { get; set; } = new PlayerProfileToMicProfileMap();
    public Dictionary<PlayerProfile, string> PlayerProfileToVoiceNameMap { get; set; } = new Dictionary<PlayerProfile, string>();
    public float PositionInSongInMillis { get; set; }
    public bool IsRestart { get; set; }
    public int NextBeatToScore { get; set; }
    public Dictionary<PlayerProfile, PlayerScoreControllerData> PlayerProfileToScoreDataMap { get; set; } = new Dictionary<PlayerProfile, PlayerScoreControllerData>();
}
