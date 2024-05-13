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

    public double PositionInMillis { get; set; }
    public bool IsRestart { get; set; }
    public bool StartPaused { get; set; }
    public Dictionary<PlayerProfile, List<ISingingResultsPlayerScore>> PlayerProfileToScoreDataMap { get; set; } = new();

    public SingSceneData()
    {
    }

    public SingSceneData(SingSceneData other)
    {
        SongMetas = new(other.SongMetas);
        SingScenePlayerData = new(other.SingScenePlayerData);
        MedleySongIndex = other.MedleySongIndex;
        PositionInMillis = other.PositionInMillis;
        IsRestart = other.IsRestart;
        PlayerProfileToScoreDataMap = new(other.PlayerProfileToScoreDataMap);
        partyModeSceneData = other.partyModeSceneData;
    }
}
