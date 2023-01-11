using System;
using System.Collections.Generic;

[Serializable]
public class GameRoundData
{
    public List<SongMeta> SongMetas { get; set; } = new();
    public SingScenePlayerData SingScenePlayerData { get; set; } = new();
    public GameModifierData GameModifierData { get; set; } = new();
    public bool IsMedley { get; set; }
}
