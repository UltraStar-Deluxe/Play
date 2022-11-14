using System;

[Serializable]
public class SongSelectSceneData : SceneData
{
    public SongMeta SongMeta { get; set; }
    public SongMeta[] SongMetaSet { get; set; }
    public bool IsPartyMode { get; set; }
}
