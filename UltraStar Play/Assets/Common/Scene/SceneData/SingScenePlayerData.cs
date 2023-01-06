using System.Collections.Generic;

public class SingScenePlayerData
{
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; } = new();
    public Dictionary<PlayerProfile, MicProfile> PlayerProfileToMicProfileMap { get; set; } = new();
    public Dictionary<PlayerProfile, string> PlayerProfileToVoiceNameMap { get; set; } = new();
}
