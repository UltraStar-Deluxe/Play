using System;
using System.Collections.Generic;

[Serializable]
public class SingSceneData : SceneData
{
    public List<SongMeta> SongMetas { get; set; }
    public SingScenePlayerData SingScenePlayerData { get; set; } = new();

    public int MedleySongIndex { get; set; } = -1;
    public bool IsMedley => MedleySongIndex >= 0;
    public GameRoundSettings gameRoundSettings = new();

    public PartyModeSceneData partyModeSceneData;

    public double PositionInSongInMillis { get; set; }
    public bool IsRestart { get; set; }
    public int NextBeatToScore { get; set; }
    public Dictionary<PlayerProfile, List<PlayerScoreControlData>> PlayerProfileToScoreDataMap { get; set; } = new();

    public SingSceneData()
    {
    }

    public SingSceneData(SingSceneData other)
    {
        SongMetas = new(other.SongMetas);
        SingScenePlayerData = new(other.SingScenePlayerData);
        MedleySongIndex = other.MedleySongIndex;
        PositionInSongInMillis = other.PositionInSongInMillis;
        IsRestart = other.IsRestart;
        NextBeatToScore = other.NextBeatToScore;
        PlayerProfileToScoreDataMap = new(other.PlayerProfileToScoreDataMap);
        partyModeSceneData = other.partyModeSceneData;
    }
}
