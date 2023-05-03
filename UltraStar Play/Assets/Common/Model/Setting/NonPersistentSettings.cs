using System.Collections.Generic;

public class NonPersistentSettings
{
    public GameRoundSettings GameRoundSettings { get; set; } = new();
    
    // Song select settings
    public string playlistName = "";
    public bool micTestActive;
    public Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> activeSearchPropertyFilters = new();
    public bool isShowOnlyDuetsFilterActive;

    // Song editor settings
    public float SongEditorMusicPlaybackSpeed { get; set; } = 1;
    public bool IsSongEditorRecordingEnabled { get; set; }
}
