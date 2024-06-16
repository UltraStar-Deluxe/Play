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
    private readonly Dictionary<PlayerProfile, ISingingResultsPlayerScore> playerProfileToScoreData = new();
    public PartyModeSceneData partyModeSceneData;
    public SceneData lastSceneData;
    public GameRoundSettings GameRoundSettings { get; set; } = new();

    public void AddPlayerScores(PlayerProfile profile, ISingingResultsPlayerScore score)
    {
        if (!PlayerProfiles.Contains(profile))
        {
            PlayerProfiles.Add(profile);
        }
        playerProfileToScoreData[profile] = score;
    }

    public ISingingResultsPlayerScore GetPlayerScores(PlayerProfile playerProfile)
    {
        if (playerProfile == null)
        {
            return null;
        }
        return playerProfileToScoreData[playerProfile];
    }
}
