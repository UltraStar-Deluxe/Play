using System;
using System.Collections.Generic;

[Serializable]
public class SingingResultsSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public List<PlayerProfile> PlayerProfiles { get; set; } = new List<PlayerProfile>();
    private Dictionary<PlayerProfile, PlayerScoreData> playerScoreMap = new Dictionary<PlayerProfile, PlayerScoreData>();

    public void AddPlayerScores(PlayerProfile profile, PlayerScoreData scoreData)
    {
        if (!PlayerProfiles.Contains(profile))
        {
            PlayerProfiles.Add(profile);
        }
        playerScoreMap.Add(profile, scoreData);
    }

    public PlayerScoreData GetPlayerScores(PlayerProfile playerProfile)
    {
        return playerScoreMap[playerProfile];
    }

    public class PlayerScoreData
    {
        public double TotalScore { get; set; }
        public double NormalNotesScore { get; set; }
        public double GoldenNotesScore { get; set; }
        public double PerfectSentenceBonusScore { get; set; }
    }
}