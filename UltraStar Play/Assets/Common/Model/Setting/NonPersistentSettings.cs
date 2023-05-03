using System.Collections.Generic;
using UniRx;

public class NonPersistentSettings
{
    public GameRoundSettings GameRoundSettings { get; set; } = new();
    
    // Song select settings
    public ReactiveProperty<string> PlaylistName { get; private set; } = new("");
    public ReactiveProperty<bool> MicTestActive { get; private set; } = new();
    public readonly Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> activeSearchPropertyFilters = new();
    public ReactiveProperty<bool> IsShowOnlyDuetsFilterActive { get; private set; } = new();

    // Song editor settings
    public ReactiveProperty<float> SongEditorMusicPlaybackSpeed { get; set; } = new(1);
    public ReactiveProperty<bool> IsSongEditorRecordingEnabled { get; set; } = new();
}
