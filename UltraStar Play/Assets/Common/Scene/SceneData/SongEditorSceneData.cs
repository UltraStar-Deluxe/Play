using System;
using System.Collections.Generic;

[Serializable]
public class SongEditorSceneData : SceneData
{
    public EScene PreviousScene { get; set; }
    public SceneData PreviousSceneData { get; set; }

    public SongMeta SongMeta { get; set; }
    public double PositionInMillis { get; set; }
    public List<PlayerProfile> SelectedPlayerProfiles { get; set; } = new();
    public Dictionary<PlayerProfile, MicProfile> PlayerProfileToMicProfileMap { get; set; } = new();
    public bool CreateSingAlongDataViaAiTools { get; set; }
}
