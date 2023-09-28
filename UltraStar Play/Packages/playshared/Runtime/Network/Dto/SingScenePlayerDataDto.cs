using System.Collections.Generic;

public class SingScenePlayerDataDto : JsonSerializable
{
    public List<string> PlayerProfileNames { get; set; } = new();
    public Dictionary<string, MicProfileDto> PlayerProfileToMicProfileMap { get; set; } = new();
    // TODO: Should be called PlayerProfileToVoiceIdMap
    public Dictionary<string, EExtendedVoiceId> PlayerProfileToVoiceNameMap { get; set; } = new();
}
