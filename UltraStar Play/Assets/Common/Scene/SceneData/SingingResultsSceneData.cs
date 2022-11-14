using System;
using System.Collections.Generic;

[Serializable]
public class SingingResultsSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public int SongDurationInMillis { get; set; }
    public List<PlayerProfile> PlayerProfiles { get; set; } = new();
    public Dictionary<PlayerProfile, MicProfile> PlayerProfileToMicProfileMap { get; set; } = new();
    private readonly Dictionary<PlayerProfile, PlayerScoreControlData> playerScoreMap = new();
    public bool IsPartyMode { get; set; }

    public void AddPlayerScores(PlayerProfile profile, PlayerScoreControlData scoreData)
    {
        if (!PlayerProfiles.Contains(profile))
        {
            PlayerProfiles.Add(profile);
        }
        playerScoreMap[profile] = scoreData;
    }

    public PlayerScoreControlData GetPlayerScores(PlayerProfile playerProfile)
    {
        return playerScoreMap[playerProfile];
    }
}
