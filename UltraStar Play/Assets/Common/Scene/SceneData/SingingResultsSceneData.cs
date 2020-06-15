using System;
using System.Collections.Generic;

[Serializable]
public class SingingResultsSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public int SongDurationInMillis { get; set; }
    public List<PlayerProfile> PlayerProfiles { get; set; } = new List<PlayerProfile>();
    public PlayerProfileToMicProfileMap PlayerProfileToMicProfileMap { get; set; } = new PlayerProfileToMicProfileMap();
    private readonly Dictionary<PlayerProfile, PlayerScoreControllerData> playerScoreMap = new Dictionary<PlayerProfile, PlayerScoreControllerData>();

    public void AddPlayerScores(PlayerProfile profile, PlayerScoreControllerData scoreData)
    {
        if (!PlayerProfiles.Contains(profile))
        {
            PlayerProfiles.Add(profile);
        }
        playerScoreMap[profile] = scoreData;
    }

    public PlayerScoreControllerData GetPlayerScores(PlayerProfile playerProfile)
    {
        return playerScoreMap[playerProfile];
    }
}
