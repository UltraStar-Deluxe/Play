using System;
using System.Collections.Generic;

[Serializable]
public class SingingResultsSceneData : SceneData
{
    public List<SongMeta> SongMetas { get; set; }
    public bool IsMedley { get; set; }
    public int SongDurationInMillis { get; set; }
    public List<PlayerProfile> PlayerProfiles { get; set; } = new();
    public Dictionary<PlayerProfile, MicProfile> PlayerProfileToMicProfileMap { get; set; } = new();
    private readonly Dictionary<PlayerProfile, PlayerScoreControlData> playerScoreMap = new();
    public PartyModeSceneData partyModeSceneData;
    public SceneData lastSceneData;
    public GameRoundSettings GameRoundSettings { get; set; } = new();

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
        if (playerProfile == null)
        {
            return null;
        }
        return playerScoreMap[playerProfile];
    }
}
