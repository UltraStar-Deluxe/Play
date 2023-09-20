using System.Collections.Generic;

public class SingScenePlayerDataDto : JsonSerializable
{
    public List<string> PlayerProfileNames { get; set; } = new();
    public Dictionary<string, MicProfileDto> PlayerProfileToMicProfileMap { get; set; } = new();
    public Dictionary<string, string> PlayerProfileToVoiceIdMap { get; set; } = new();
}
