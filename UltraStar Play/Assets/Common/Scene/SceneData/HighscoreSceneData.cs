using System;

[Serializable]
public class HighscoreSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public EDifficulty Difficulty { get; set; }
    public PartyModeSceneData partyModeSceneData;
}
