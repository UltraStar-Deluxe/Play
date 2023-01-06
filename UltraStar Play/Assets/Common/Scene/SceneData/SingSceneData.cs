using System;
using System.Collections.Generic;

[Serializable]
public class SingSceneData : SceneData
{
    public SongMeta SelectedSongMeta { get; set; }
    public SingScenePlayerData SingScenePlayerData { get; set; } = new();

    public double PositionInSongInMillis { get; set; }
    public bool IsRestart { get; set; }
    public int NextBeatToScore { get; set; }
    public Dictionary<PlayerProfile, PlayerScoreControlData> PlayerProfileToScoreDataMap { get; set; } = new();
}
