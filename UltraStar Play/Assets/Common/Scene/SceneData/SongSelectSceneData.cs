using System;

[Serializable]
public class SongSelectSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public PartyModeSceneData partyModeSceneData;
}
