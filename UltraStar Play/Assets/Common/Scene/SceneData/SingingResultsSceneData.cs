using System;
using System.Collections.Generic;

[Serializable]
public class SingingResultsSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public List<PlayerProfile> PlayerProfiles { get; set; } = new List<PlayerProfile>();
    public PlayerProfileToMicProfileMap PlayerProfileToMicProfileMap { get; set; } = new PlayerProfileToMicProfileMap();
    private readonly Dictionary<PlayerProfile, PlayerScoreResultData> playerScoreMap = new Dictionary<PlayerProfile, PlayerScoreResultData>();

    public void AddPlayerScores(PlayerProfile profile, PlayerScoreResultData scoreData)
    {
        if (!PlayerProfiles.Contains(profile))
        {
            PlayerProfiles.Add(profile);
        }
        playerScoreMap[profile] = scoreData;
    }

    public PlayerScoreResultData GetPlayerScores(PlayerProfile playerProfile)
    {
        return playerScoreMap[playerProfile];
    }

    public class PlayerScoreResultData
    {
        public double TotalScore { get; set; }
        public double NormalNotesScore { get; set; }
        public double GoldenNotesScore { get; set; }
        public double PerfectSentenceBonusScore { get; set; }
    }
}
