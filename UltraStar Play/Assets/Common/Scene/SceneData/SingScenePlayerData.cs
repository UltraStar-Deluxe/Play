using System.Collections.Generic;

public class SingScenePlayerData
{
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; } = new();
    public Dictionary<PlayerProfile, MicProfile> PlayerProfileToMicProfileMap { get; set; } = new();
    public Dictionary<PlayerProfile, EExtendedVoiceId> PlayerProfileToVoiceIdMap { get; set; } = new();

    public SingScenePlayerData()
    {
    }

    public SingScenePlayerData(SingScenePlayerData other)
    {
        SelectedPlayerProfiles = new(other.SelectedPlayerProfiles);
        PlayerProfileToMicProfileMap = new(other.PlayerProfileToMicProfileMap);
        PlayerProfileToVoiceIdMap = new(other.PlayerProfileToVoiceIdMap);
    }
}
