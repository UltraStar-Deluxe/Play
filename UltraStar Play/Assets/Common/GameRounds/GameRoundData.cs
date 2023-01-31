using System;
using System.Collections.Generic;

[Serializable]
public class GameRoundData
{
    public List<SongMeta> SongMetas { get; set; } = new();
    public SingScenePlayerData SingScenePlayerData { get; set; } = new();
    public GameRoundSettings GameRoundSettings { get; set; } = new();
    public bool IsMedley { get; set; }
}
