using System.Collections.Generic;

public class GameRoundDataDto : JsonSerializable
{
    public List<string> SongIds { get; set; } = new();
    public SingScenePlayerDataDto SingScenePlayerDataDto { get; set; } = new();
    public GameRoundSettings GameRoundSettings { get; set; } = new();
    public bool IsMedley { get; set; }
}
