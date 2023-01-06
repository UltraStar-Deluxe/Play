using System;
using System.Collections.Generic;

[Serializable]
public class GameRoundData
{
    public IReadOnlyList<SongMeta> SongMetas { get; private set; }
    public SingScenePlayerData SingScenePlayerData { get; private set; }
    public GameModifierData GameModifierData { get; private set; }
    public bool IsMedley => SongMetas.Count > 1;

    public GameRoundData(List<SongMeta> songMetas, SingScenePlayerData singScenePlayerData, GameModifierData gameModifierData)
    {
        SongMetas = songMetas;
        SingScenePlayerData = singScenePlayerData;
        GameModifierData = gameModifierData;
    }
}
